namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class TaskRunner<TRunnable> : ITask<TRunnable>
        where TRunnable : IRunnable
    {
        private readonly EventWaitHandle runImmediately = new AutoResetEvent(false);

        private ILogger logger;

        private Task mainTask;

        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRunner{TRunnable}"/> class.
        /// </summary>
        /// <param name="loggerFactory">Фабрика для создания логгера</param>
        /// <param name="serviceScopeFactory">Фабрика для создания Scope (при запуске задачи)</param>
        public TaskRunner(ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = loggerFactory.CreateLogger($"{this.GetType().Namespace}.{nameof(TaskRunner<TRunnable>)}<{typeof(TRunnable).FullName}>");
            ServiceScopeFactory = serviceScopeFactory;
            RunStatus = new TaskRunStatus();
        }

        /// <inheritdoc />
        public event EventHandler<ServiceProviderEventArgs> BeforeRun;

        /// <inheritdoc />
        public event EventHandler<ExceptionEventArgs> AfterRunFail;

        /// <inheritdoc />
        public event EventHandler<ServiceProviderEventArgs> AfterRunSuccess;

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
        public CultureInfo RunningCulture { get; set; }

        /// <inheritdoc />
        public TimeSpan Interval { get; set; }

        private IServiceScopeFactory ServiceScopeFactory { get; set; }

        /// <inheritdoc />
        public void Start(TimeSpan firstRunDelay)
        {
            if (firstRunDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(firstRunDelay), "First run delay can't be negative");
            }

            if (Interval < TimeSpan.Zero)
            {
                throw new InvalidOperationException("Interval can't be negative");
            }

            logger.LogInformation("Start() called...");
            if (mainTask != null)
            {
                throw new InvalidOperationException("Already started");
            }

            cancellationTokenSource = new CancellationTokenSource();
            mainTask = Task.Run(() => MainLoop(firstRunDelay));
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

        protected void MainLoop(TimeSpan firstRunDelay)
        {
            logger.LogInformation("MainLoop() started. Running...");
            var events = new WaitHandle[] { cancellationTokenSource.Token.WaitHandle, runImmediately };
            var sleepInterval = firstRunDelay;
            while (true)
            {
                logger.LogDebug("Sleeping for {0}...", sleepInterval);
                RunStatus.NextRunTime = DateTimeOffset.Now.Add(sleepInterval);
                var signaled = WaitHandle.WaitAny(events, sleepInterval);

                // index of signalled handler. zero is for 'cancellationToken'
                if (signaled == 0)
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
                    if (RunningCulture != null)
                    {
                        logger.LogDebug("Switching to {0} CultureInfo...", RunningCulture.Name);
#if NET451
                        Thread.CurrentThread.CurrentCulture = RunningCulture;
                        Thread.CurrentThread.CurrentUICulture = RunningCulture;
#else
                        CultureInfo.CurrentCulture = RunningCulture;
                        CultureInfo.CurrentUICulture = RunningCulture;
#endif
                    }

                    try
                    {
                        OnBeforeRun(scope.ServiceProvider);

                        IsRunningRightNow = true;

                        var startTime = DateTimeOffset.Now;

                        var runnable = (TRunnable)scope.ServiceProvider.GetRequiredService(typeof(TRunnable));

                        logger.LogInformation("Calling Run()...");
                        runnable.Run(this, cancellationTokenSource.Token);
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

                if (Interval.Ticks == 0)
                {
                    logger.LogWarning("Interval equal to zero. Stopping...");
                    cancellationTokenSource.Cancel();
                }
                else
                {
                    sleepInterval = Interval; // return to normal (important after first run only)
                }
            }

            logger.LogInformation("MainLoop() finished.");
        }

        /// <summary>
        /// Invokes <see cref="BeforeRun"/> event (don't forget to call base.OnBeforeRun in override)
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to be passed in event args</param>
        protected virtual void OnBeforeRun(IServiceProvider serviceProvider)
        {
            BeforeRun?.Invoke(this, new ServiceProviderEventArgs(serviceProvider));
        }

        /// <summary>
        /// Invokes <see cref="AfterRunSuccess"/> event (don't forget to call base.OnAfterRunSuccess in override)
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> to be passed in event args</param>
        /// <remarks>
        /// Attention! Any exception, catched during AfterRunSuccess.Invoke, is written to error log and ignored.
        /// </remarks>
        protected virtual void OnAfterRunSuccess(IServiceProvider serviceProvider)
        {
            try
            {
                AfterRunSuccess?.Invoke(this, new ServiceProviderEventArgs(serviceProvider));
            }
            catch (Exception ex2)
            {
                logger.LogError(0, ex2, "Error while processing AfterRunSuccess event (ignored)");
            }
        }

        /// <summary>
        /// Invokes <see cref="AfterRunFail"/> event - don't forget to call base.OnAfterRunSuccess in override
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
                AfterRunFail?.Invoke(this, new ExceptionEventArgs(serviceProvider, ex));
            }
            catch (Exception ex2)
            {
                logger.LogError(0, ex2, "Error while processing AfterRunFail event (ignored)");
            }
        }
    }
}
