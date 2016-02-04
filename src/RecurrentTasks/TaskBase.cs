namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;

    public abstract class TaskBase<TState> : ITask where TState : TaskStatus, new()
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
            Status = new TState();
        }

        TaskStatus ITask.Status { get { return Status; } }

        public TState Status { get; private set; }

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
                Status.NextRunTime = DateTimeOffset.Now.Add(sleepInterval);
                var signaled = WaitHandle.WaitAny(events, sleepInterval);
                if (signaled == 0) // индекс сработавшего. нулевой это breakEvent
                {
                    // значит закругляемся
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
                        IsRunningRightNow = true;

                        Status.LastRunTime = DateTimeOffset.Now;

                        Logger.LogInformation("Calling Run()...");
                        Run(scope.ServiceProvider, Status);
                        Logger.LogInformation("Done.");

                        Status.LastRunResult = TaskRunResult.Success;
                        Status.LastSuccessTime = DateTimeOffset.Now;
                        Status.FirstFail = DateTimeOffset.MinValue;
                        Status.FailsCount = 0;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Ooops, error (ignoring):", ex);
                        Status.LastRunResult = TaskRunResult.Fail;
                        Status.LastException = ex;
                        if (Status.FailsCount == 0)
                        {
                            Status.FirstFail = DateTimeOffset.Now;
                        }
                        Status.FailsCount++;
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

        protected abstract void Run(IServiceProvider serviceProvider, TState state);
    }
}
