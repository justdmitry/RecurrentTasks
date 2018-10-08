namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    public class TaskOptions
    {
        /// <summary>
        /// If non-null, current thread culture will be set to this value before <see cref="IRunnable.RunAsync"/> is called
        /// </summary>
        public CultureInfo RunCulture { get; set; }

        /// <summary>
        /// Auto-start task when <see cref="IHostedService.StartAsync"/> is called (default <b>true</b>)
        /// </summary>
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// Task run interval
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// First run delay (to prevent app freeze during startup due to many tasks initialization).
        /// Default is random value from 10 to 30 seconds.
        /// </summary>
        public TimeSpan FirstRunDelay { get; set; } = TimeSpan.FromSeconds(new Random().Next(10, 30));

        /// <summary>
        /// Return <b>false</b> to cancel/skip task run
        /// </summary>
        public Func<IServiceProvider, ITask, Task<bool>> BeforeRun { get; set; }

        public Func<IServiceProvider, ITask, Task> AfterRunSuccess { get; set; }

        public Func<IServiceProvider, ITask, Exception, Task> AfterRunFail { get; set; }
    }
}
