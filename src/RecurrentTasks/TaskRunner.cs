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
        private readonly EventWaitHandle runImmediately = new AutoResetEvent(false);

        private readonly ILogger logger;

        private Task mainTask;

        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRunner{TRunnable}"/> class.
        /// </summary>
        /// <param name="loggerFactory">Фабрика для создания логгера</param>
        /// <param name="options">TaskOptions</param>
        /// <param name="serviceScopeFactory">Фабрика для создания Scope (при запуске задачи)</param>
        public TaskRunner(ILoggerFactory loggerFactory, TaskOptions<TRunnable> options, IServiceScopeFactory serviceScopeFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (serviceScopeFactory == null)
            {
                throw new ArgumentNullException(nameof(serviceScopeFactory));
            }

            this.logger = loggerFactory.CreateLogger($"{this.GetType().Namespace}.{nameof(TaskRunner<TRunnable>)}<{typeof(TRunnable).FullName}>");
            Options = options;
            ServiceScopeFactory = serviceScopeFactory;
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

        private IServiceScopeFactory ServiceScopeFactory { get; set; }

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

            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            mainTask = Task.Run(() => MainLoop(Options.FirstRunDelay, cancellationTokenSource.Token));
        }

        /// <inheritdoc />
        public void Stop()
        {
            logger.LogInformation("Stop() called...");
            if (mainTask == null)
            {
                throw new InvalidOperationException("Can't stop without start");
            }

            cancellationTokenSource.Cancel();
        }

        /// <inheritdoc />
        public void TryRunImmediately()
        {
            if (mainTask == null)
            {
                throw new InvalidOperationException("Can't run without Start");
            }

            runImmediately.Set();
        }

        protected void MainLoop(TimeSpan firstRunDelay, CancellationToken cancellationToken)
        {
            logger.LogInformation("MainLoop() started. Running...");
            var sleepInterval = firstRunDelay;
            var handles = new[] { cancellationToken.WaitHandle, runImmediately };
            while (true)
            {
                logger.LogDebug("Sleeping for {0}...", sleepInterval);
                RunStatus.NextRunTime = DateTimeOffset.Now.Add(sleepInterval);
                WaitHandle.WaitAny(handles, sleepInterval);

                if (cancellationToken.IsCancellationRequested)
                {
                    // must stop and quit
                    logger.LogWarning("CancellationToken signaled, stopping...");
                    mainTask = null;
                    cancellationTokenSource = null;
                    break;
                }

                logger.LogDebug("It is time! Creating scope...");
                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    if (Options.RunCulture != null)
                    {
                        logger.LogDebug("Switching to {0} CultureInfo...", Options.RunCulture.Name);
                        CultureInfo.CurrentCulture = Options.RunCulture;
                        CultureInfo.CurrentUICulture = Options.RunCulture;
                    }

                    try
                    {
                        var beforeRunResponse = OnBeforeRun(scope.ServiceProvider);

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
                            runnable.RunAsync(this, scope.ServiceProvider, cancellationToken).GetAwaiter().GetResult();
                            logger.LogInformation("Done.");

                            RunStatus.LastRunTime = startTime;
                            RunStatus.LastResult = TaskRunResult.Success;
                            RunStatus.LastSuccessTime = DateTimeOffset.Now;
                            RunStatus.FirstFailTime = DateTimeOffset.MinValue;
                            RunStatus.FailsCount = 0;
                            RunStatus.LastException = null;
                            IsRunningRightNow = false;

                            OnAfterRunSuccess(scope.ServiceProvider);
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

                        OnAfterRunFail(scope.ServiceProvider, ex);
                    }
                    finally
                    {
                        IsRunningRightNow = false;
                    }
                }

                if (Options.Interval.Ticks == 0)
                {
                    logger.LogWarning("Interval equal to zero. Stopping...");
                    cancellationTokenSource.Cancel();
                }
                else
                {
                    sleepInterval = Options.Interval; // return to normal (important after first run only)
                }
            }

            logger.LogInformation("MainLoop() finished.");
        }

        /// <summary>
        /// Invokes <see cref="BeforeRunAsync"/> handler (don't forget to call base.OnBeforeRun in override)
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to be passed in event args</param>
        /// <returns>Return <b>falce</b> to cancel/skip task run</returns>
        protected virtual bool OnBeforeRun(IServiceProvider serviceProvider)
        {
            return Options.BeforeRun?.Invoke(serviceProvider, this).GetAwaiter().GetResult() ?? true;
        }

        /// <summary>
        /// Invokes <see cref="AfterRunSuccessAsync"/> handler (don't forget to call base.OnAfterRunSuccess in override)
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to be passed in event args</param>
        /// <remarks>
        /// Attention! Any exception, catched during AfterRunSuccess.Invoke, is written to error log and ignored.
        /// </remarks>
        protected virtual void OnAfterRunSuccess(IServiceProvider serviceProvider)
        {
            try
            {
                Options.AfterRunSuccess?.Invoke(serviceProvider, this).GetAwaiter().GetResult();
            }
            catch (Exception ex2)
            {
                logger.LogError(0, ex2, "Error while processing AfterRunSuccess event (ignored)");
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
        protected virtual void OnAfterRunFail(IServiceProvider serviceProvider, Exception ex)
        {
            try
            {
                Options.AfterRunFail?.Invoke(serviceProvider, this, ex).GetAwaiter().GetResult();
            }
            catch (Exception ex2)
            {
                logger.LogError(0, ex2, "Error while processing AfterRunFail event (ignored)");
            }
        }
    }
}
