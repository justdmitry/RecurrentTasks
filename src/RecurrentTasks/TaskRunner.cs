namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public class TaskRunner<TRunnable> : ITask<TRunnable> 
        where TRunnable: IRunnable
    {
        private readonly EventWaitHandle breakEvent = new ManualResetEvent(false);

        private readonly EventWaitHandle runImmediately = new AutoResetEvent(false);

        private ILogger logger;

        private Task mainTask;

        /// <param name="loggerFactory">Фабрика для создания логгера</param>
        /// <param name="interval">Интервал (периодичность) запуска задачи</param>
        /// <param name="serviceScopeFactory">Фабрика для создания Scope (при запуске задачи)</param>
        public TaskRunner(ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
            ServiceScopeFactory = serviceScopeFactory;
            RunStatus = new TaskRunStatus();
        }

        public event EventHandler<ExceptionEventArgs> AfterRunFail;

        TaskRunStatus ITask.RunStatus { get { return RunStatus; } }

        public TaskRunStatus RunStatus { get; protected set; }

        public bool IsStarted
        {
            get
            {
                return mainTask != null;
            }
        }

        public bool IsRunningRightNow { get; private set; }

        public CultureInfo RunningCulture { get; set; }

        public TimeSpan Interval { get; set; }

        private IServiceScopeFactory ServiceScopeFactory { get; set; }

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
            breakEvent.Reset();
            mainTask = Task.Run(() => MainLoop(firstRunDelay));
        }

        public void Stop()
        {
            logger.LogInformation("Stop() called...");
            if (mainTask == null)
            {
                throw new InvalidOperationException("Can't stop without start");
            }
            breakEvent.Set();
        }

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
            var events = new WaitHandle[] { breakEvent, runImmediately };
            var sleepInterval = firstRunDelay;
            while (true)
            {
                logger.LogDebug("Sleeping for {0}...", sleepInterval);
                RunStatus.NextRunTime = DateTimeOffset.Now.Add(sleepInterval);
                var signaled = WaitHandle.WaitAny(events, sleepInterval);
                if (signaled == 0) // index of signalled. zero is for 'breakEvent'
                {
                    // must stop and quit
                    logger.LogWarning("BreakEvent is set, stopping...");
                    mainTask = null;
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
                        OnBeforeRun();

                        IsRunningRightNow = true;

                        var startTime = DateTimeOffset.Now;

                        var runnable = (TRunnable) scope.ServiceProvider.GetRequiredService(typeof(TRunnable));

                        logger.LogInformation("Calling Run()...");
                        runnable.Run(this);
                        logger.LogInformation("Done.");

                        RunStatus.LastRunTime = startTime;
                        RunStatus.LastResult = TaskRunResult.Success;
                        RunStatus.LastSuccessTime = DateTimeOffset.Now;
                        RunStatus.FirstFailTime = DateTimeOffset.MinValue;
                        RunStatus.FailsCount = 0;
                        RunStatus.LastException = null;
                        IsRunningRightNow = false;

                        OnAfterRunSuccess();
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

                        OnAfterRunFail();

                        try
                        {
                            AfterRunFail?.Invoke(this, new ExceptionEventArgs(ex));
                        }
                        catch (Exception ex2)
                        {
                            logger.LogError(0, ex2, "Error while processing AfterRunFail event (ignored)");
                        }
                    }
                    finally
                    {
                        IsRunningRightNow = false;
                    }
                }
                if (Interval.Ticks == 0)
                {
                    logger.LogWarning("Interval equal to zero. Stopping...");
                    breakEvent.Set();
                }
                else
                {
                    sleepInterval = Interval; // return to normal (important after first run only)
                }
            }
            logger.LogInformation("MainLoop() finished.");
        }

        /// <summary>
        /// Called before Run() is called (even before IsRunningRightNow set to true)
        /// </summary>
        protected virtual void OnBeforeRun()
        {
            // nothing
        }

        /// <summary>
        /// Called after Run() sucessfully finished (after IsRunningRightNow set to false)
        /// </summary>
        protected virtual void OnAfterRunSuccess()
        {
            // nothing
        }

        /// <summary>
        /// Called after Run() falied (after IsRunningRightNow set to false)
        /// </summary>
        protected virtual void OnAfterRunFail()
        {
            // nothing
        }
    }
}
