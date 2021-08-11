namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

    public class TaskOptions
    {
        /// <summary>
        /// Maximum allowed <see cref="Interval"/> and <see cref="FirstRunDelay"/> values.
        /// </summary>
        public static readonly TimeSpan MaxInterval = TimeSpan.FromMilliseconds(int.MaxValue);

        private TimeSpan interval;
        private TimeSpan firstRunDelay = TimeSpan.FromSeconds(new Random().Next(10, 30));

        /// <summary>
        /// If non-null, current thread culture will be set to this value before <see cref="IRunnable.RunAsync"/> is called
        /// </summary>
        public CultureInfo RunCulture { get; set; }

        /// <summary>
        /// Auto-start task when <see cref="IHostedService.StartAsync"/> is called (default <b>true</b>)
        /// </summary>
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// Task run interval.
        /// </summary>
        public TimeSpan Interval
        {
            get
            {
                return interval;
            }

            set
            {
                if (value > MaxInterval)
                {
                    throw new ArgumentOutOfRangeException(nameof(Interval), "Must be less than Int32.MaxValue milliseconds (approx. 24 days 20 hours)");
                }

                interval = value;
            }
        }

        /// <summary>
        /// First run delay (to prevent app freeze during startup due to many tasks initialization).
        /// Default is random value from 10 to 30 seconds.
        /// </summary>
        public TimeSpan FirstRunDelay
        {
            get
            {
                return interval;
            }

            set
            {
                if (value > MaxInterval)
                {
                    throw new ArgumentOutOfRangeException(nameof(FirstRunDelay), "Must be less than Int32.MaxValue milliseconds (approx. 24 days 20 hours)");
                }

                interval = value;
            }
        }


        /// <summary>
        /// Return <b>false</b> to cancel/skip task run
        /// </summary>
        public Func<IServiceProvider, ITask, Task<bool>> BeforeRun { get; set; }

        public Func<IServiceProvider, ITask, Task> AfterRunSuccess { get; set; }

        public Func<IServiceProvider, ITask, Exception, Task> AfterRunFail { get; set; }
    }
}
