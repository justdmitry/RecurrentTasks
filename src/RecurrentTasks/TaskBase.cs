namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public abstract class TaskBase<TRunStatus> : ITask where TRunStatus : TaskRunStatus, new()
    {
        private static readonly Random Random = new Random();

        private readonly EventWaitHandle breakEvent = new ManualResetEvent(false);

        private readonly EventWaitHandle runImmediately = new AutoResetEvent(false);

        private Task mainTask;

        /// <param name="loggerFactory">Фабрика для создания логгера</param>
        /// <param name="interval">Интервал (периодичность) запуска задачи</param>
        /// <param name="serviceScopeFactory">Фабрика для создания Scope (при запуске задачи)</param>
        public TaskBase(ILoggerFactory loggerFactory, TimeSpan interval, IServiceScopeFactory serviceScopeFactory)
        {
            Logger = loggerFactory.CreateLogger(this.GetType().FullName);
            Interval = interval;
            ServiceScopeFactory = serviceScopeFactory;
            RunStatus = new TRunStatus();
        }

        TaskRunStatus ITask.RunStatus { get { return RunStatus; } }

        public TRunStatus RunStatus { get; protected set; }

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

        /// <summary>
        /// Current logger
        /// </summary>
        protected ILogger Logger { get; private set; }

        private IServiceScopeFactory ServiceScopeFactory { get; set; }

        public void Start()
        {
            Start(TimeSpan.FromSeconds(Random.Next(10, 30)));
        }

        public void Start(TimeSpan initialTimeout)
        {
            Logger.LogInformation("Start() called...");
            if (mainTask != null)
            {
                throw new InvalidOperationException("Already started");
            }
            breakEvent.Reset();
            mainTask = Task.Run(() => MainLoop(initialTimeout));
        }

        public void Stop()
        {
            Logger.LogInformation("Stop() called...");
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

        protected void MainLoop(TimeSpan initialTimeout)
        {
            Logger.LogInformation("MainLoop() started. Running...");
            var events = new WaitHandle[] { breakEvent, runImmediately };
            var sleepInterval = initialTimeout;
            while (true)
            {
                Logger.LogDebug("Sleeping for {0}...", sleepInterval);
                RunStatus.NextRunTime = DateTimeOffset.Now.Add(sleepInterval);
                var signaled = WaitHandle.WaitAny(events, sleepInterval);
                if (signaled == 0) // index of signalled. zero is for 'breakEvent'
                {
                    // must stop and quit
                    Logger.LogWarning("BreakEvent is set, stopping...");
                    mainTask = null;
                    break;
                }
                Logger.LogDebug("It is time! Creating scope...");
                using (var scope = ServiceScopeFactory.CreateScope())
                {
                    if (RunningCulture != null)
                    {
                        Logger.LogDebug("Switching to {0} CultureInfo...", RunningCulture.Name);
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

                        RunStatus.LastRunTime = DateTimeOffset.Now;

                        Logger.LogInformation("Calling Run()...");
                        Run(scope.ServiceProvider, RunStatus);
                        Logger.LogInformation("Done.");

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
                        Logger.LogWarning("Ooops, error (ignoring, see RunStatus.LastException):", ex);
                        RunStatus.LastResult = TaskRunResult.Fail;
                        RunStatus.LastException = ex;
                        if (RunStatus.FailsCount == 0)
                        {
                            RunStatus.FirstFailTime = DateTimeOffset.Now;
                        }
                        RunStatus.FailsCount++;
                        IsRunningRightNow = false;

                        OnAfterRunFail();
                    }
                    finally
                    {
                        IsRunningRightNow = false;
                    }
                }
                sleepInterval = Interval; // return to normal
            }
            Logger.LogInformation("MainLoop() finished.");
        }

        protected abstract void Run(IServiceProvider serviceProvider, TRunStatus runStatus);

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
