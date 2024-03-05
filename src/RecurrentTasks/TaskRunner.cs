namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class TaskRunner<TRunnable> : ITask<TRunnable>
        where TRunnable : IRunnable
    {
        private readonly ILogger logger;
        private readonly IServiceScopeFactory serviceScopeFactory;

        private Task? mainTask;
        private CancellationTokenSource? stopTaskSource;
        private CancellationTokenSource? waitForNextRunSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRunner{TRunnable}"/> class.
        /// </summary>
        /// <param name="loggerFactory">Фабрика для создания логгера</param>
        /// <param name="options">TaskOptions</param>
        /// <param name="serviceScopeFactory">Фабрика для создания Scope (при запуске задачи)</param>
        public TaskRunner(ILoggerFactory loggerFactory, TaskOptions<TRunnable> options, IServiceScopeFactory serviceScopeFactory)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(serviceScopeFactory);

            this.logger = options.Logger ?? loggerFactory.CreateLogger($"{this.GetType().Namespace}.{nameof(TaskRunner<TRunnable>)}<{typeof(TRunnable).FullName}>");
            Options = options;
            this.serviceScopeFactory = serviceScopeFactory;
            RunStatus = new TaskRunStatus();
        }

        /// <inheritdoc />
        public TaskRunStatus RunStatus { get; protected set; }

        /// <inheritdoc />
        public bool IsStarted
        {
            get
            {
                return mainTask != null;
            }
        }

        /// <inheritdoc />
        public bool IsRunningRightNow { get; private set; }

        /// <inheritdoc />
        public TaskOptions Options { get; }

        /// <inheritdoc />
        public Type RunnableType => typeof(TRunnable);

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            if (!IsStarted && Options.AutoStart)
            {
                Start();
            }

            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            if (IsStarted)
            {
                Stop();
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Start()
        {
            Start(CancellationToken.None);
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
            if (Options.FirstRunDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(Options.FirstRunDelay), "First run delay can't be negative");
            }

            if (Options.Interval < TimeSpan.Zero)
            {
                throw new InvalidOperationException("Interval can't be negative");
            }

            logger.LogInformation("Start() called...");
            if (mainTask != null)
            {
                throw new InvalidOperationException("Already started");
            }

            mainTask = MainLoop(Options.FirstRunDelay, cancellationToken);
        }

        /// <inheritdoc />
        public void Stop()
        {
            logger.LogInformation("Stop() called...");
            if (stopTaskSource == null)
            {
                throw new InvalidOperationException("Can't stop without start");
            }

            stopTaskSource.Cancel();
        }

        /// <inheritdoc />
        public void TryRunImmediately()
        {
            if (waitForNextRunSource == null)
            {
                throw new InvalidOperationException("Can't run without Start");
            }

            waitForNextRunSource.Cancel();
        }

        protected async Task MainLoop(TimeSpan firstRunDelay, CancellationToken cancellationToken)
        {
            logger.LogInformation("MainLoop() started. Running...");

            stopTaskSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var stopToken = stopTaskSource.Token;

            waitForNextRunSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            var waitForNextRunToken = waitForNextRunSource.Token;

            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);

            var sleepInterval = firstRunDelay;
            while (true)
            {
                logger.LogDebug("Sleeping for {0}...", sleepInterval);
                RunStatus.NextRunTime = DateTimeOffset.Now.Add(sleepInterval);

                await Task.Delay(sleepInterval, waitForNextRunToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                if (stopToken.IsCancellationRequested)
                {
                    // must stop and quit
                    logger.LogWarning("CancellationToken signaled, stopping...");
                    break;
                }

                if (waitForNextRunToken.IsCancellationRequested)
                {
                    // token and token source have been used, recreate them.
                    waitForNextRunSource?.Dispose();
                    waitForNextRunSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
                    waitForNextRunToken = waitForNextRunSource.Token;
                }

                logger.LogDebug("It is time! Creating scope...");
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    if (Options.RunCulture != null)
                    {
                        logger.LogDebug("Switching to {0} CultureInfo...", Options.RunCulture.Name);
                        CultureInfo.CurrentCulture = Options.RunCulture;
                        CultureInfo.CurrentUICulture = Options.RunCulture;
                    }

                    try
                    {
                        var beforeRunResponse = await OnBeforeRun(scope.ServiceProvider);

                        if (!beforeRunResponse)
                        {
                            logger.LogInformation("Task run cancelled (BeforeRun returned 'false')");
                        }
                        else
                        {
                            IsRunningRightNow = true;

                            var startTime = DateTimeOffset.Now;

                            var runnable = (TRunnable)scope.ServiceProvider.GetRequiredService(typeof(TRunnable));

                            logger.LogInformation("Calling Run()...");
                            await runnable.RunAsync(this, scope.ServiceProvider, stopToken);
                            logger.LogInformation("Done.");

                            RunStatus.LastRunTime = startTime;
                            RunStatus.LastResult = TaskRunResult.Success;
                            RunStatus.LastSuccessTime = DateTimeOffset.Now;
                            RunStatus.FirstFailTime = DateTimeOffset.MinValue;
                            RunStatus.FailsCount = 0;
                            RunStatus.LastException = null;
                            IsRunningRightNow = false;

                            await OnAfterRunSuccess(scope.ServiceProvider);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(0, ex, "Ooops, error (ignoring, see RunStatus.LastException or handle AfterRunFail event)");
                        RunStatus.LastResult = TaskRunResult.Fail;
                        RunStatus.LastException = ex;
                        if (RunStatus.FailsCount == 0)
                        {
                            RunStatus.FirstFailTime = DateTimeOffset.Now;
                        }

                        RunStatus.FailsCount++;
                        IsRunningRightNow = false;

                        await OnAfterRunFail(scope.ServiceProvider, ex);
                    }
                    finally
                    {
                        IsRunningRightNow = false;
                    }
                }

                if (Options.Interval.Ticks == 0)
                {
                    logger.LogWarning("Interval equal to zero. Stopping...");
                    stopTaskSource.Cancel();
                }
                else
                {
                    sleepInterval = Options.Interval; // return to normal (important after first run only)
                }
            }

            waitForNextRunSource?.Dispose();
            waitForNextRunSource = null;

            stopTaskSource?.Dispose();
            stopTaskSource = null;

            mainTask = null;

            logger.LogInformation("MainLoop() finished.");
        }

        /// <summary>
        /// Invokes <see cref="BeforeRunAsync"/> handler (don't forget to call base.OnBeforeRun in override)
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to be passed in event args</param>
        /// <returns>Return <b>falce</b> to cancel/skip task run</returns>
        protected virtual async Task<bool> OnBeforeRun(IServiceProvider serviceProvider)
        {
            if (Options.BeforeRun == null)
            {
                return true;
            }

            return await Options.BeforeRun(serviceProvider, this);
        }

        /// <summary>
        /// Invokes <see cref="AfterRunSuccessAsync"/> handler (don't forget to call base.OnAfterRunSuccess in override)
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to be passed in event args</param>
        /// <remarks>
        /// Attention! Any exception, catched during AfterRunSuccess.Invoke, is written to error log and ignored.
        /// </remarks>
        protected virtual async Task OnAfterRunSuccess(IServiceProvider serviceProvider)
        {
            if (Options.AfterRunSuccess != null)
            {
                try
                {
                    await Options.AfterRunSuccess(serviceProvider, this);
                }
                catch (Exception ex2)
                {
                    logger.LogError(0, ex2, "Error while processing AfterRunSuccess event (ignored)");
                }
            }
        }

        /// <summary>
        /// Invokes <see cref="AfterRunFailAsync"/> handler - don't forget to call base.OnAfterRunSuccess in override
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to be passed in event args</param>
        /// <param name="ex"><see cref="Exception"/> to be passes in event args</param>
        /// <remarks>
        /// Attention! Any exception, catched during AfterRunFail.Invoke, is written to error log and ignored.
        /// </remarks>
        protected virtual async Task OnAfterRunFail(IServiceProvider serviceProvider, Exception ex)
        {
            if (Options.AfterRunFail != null)
            {
                try
                {
                    await Options.AfterRunFail(serviceProvider, this, ex);
                }
                catch (Exception ex2)
                {
                    logger.LogError(0, ex2, "Error while processing AfterRunFail event (ignored)");
                }
            }
        }
    }
}
