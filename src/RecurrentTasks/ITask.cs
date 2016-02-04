namespace RecurrentTasks
{
    using System;
    using System.Globalization;

    public interface ITask
    {
        /// <summary>
        ///   <b>true</b> when task is started and will run with specified intervals
        ///   <b>false</b> when task is stopped and will NOT run
        /// </summary>
        /// <seealso cref="IsRunningRightNow"/>
        bool IsStarted { get; }

        /// <summary>
        ///   <b>true</b> when task is started and running/executing at this moment
        ///   <b>false</b> when task started, but sleeping at this moment (waiting for next run)
        /// </summary>
        /// <seealso cref="IsStarted"/>
        bool IsRunningRightNow { get; }

        /// <summary>
        /// CultureInfo to set when running (to override 'random' Culture of thread from thread pool)
        /// </summary>
        CultureInfo RunningCulture { get; set; }

        /// <summary>
        /// Information about task result (last run time, last exception, etc)
        /// </summary>
        TaskStatus Status { get; }

        /// <summary>
        /// Interval between runs
        /// </summary>
        /// <remarks>
        /// You may change this value in your Run() implementation :)
        /// </remarks>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// Start task (first run is delayed for 10-30 sec)
        /// </summary>
        /// <exception cref="InvalidOperationException">Task is already started</exception>
        void Start();

        /// <summary>
        /// Start task (and delay first run for specified interval)
        /// </summary>
        /// <exception cref="InvalidOperationException">Task is already started</exception>
        void Start(TimeSpan initialTimeout);

        /// <summary>
        /// Stop task (will NOT break if currently running)
        /// </summary>
        /// <exception cref="InvalidOperationException">Task was not started</exception>
        void Stop();

        /// <summary>
        /// Try to run task immediately
        /// </summary>
        /// <exception cref="InvalidOperationException">Task was not started</exception>
        void TryRunImmediately();
    }
}