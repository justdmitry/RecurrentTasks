namespace RecurrentTasks
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITask
    {
        /// <summary>
        /// Called before Run() is called (even before IsRunningRightNow set to true).
        /// </summary>
        Func<IServiceProvider, ITask, Task> BeforeRunAsync { get; set; }

        /// <summary>
        /// Called after Run() sucessfully finished (after IsRunningRightNow set to false)
        /// </summary>
        Func<IServiceProvider, ITask, Task> AfterRunSuccessAsync { get; set; }

        /// <summary>
        /// Called after Run() failed (after IsRunningRightNow set to false)
        /// </summary>
        Func<IServiceProvider, ITask, Exception, Task> AfterRunFailAsync { get; set; }

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
        TaskRunStatus RunStatus { get; }

        /// <summary>
        /// Interval between runs
        /// </summary>
        /// <remarks>
        /// You may change this value in your Run() implementation :)
        /// </remarks>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// Start task (and delay first run for specified interval)
        /// </summary>
        /// <param name="firstRunDelay">Delay before first task run (use TimeSpan.Zero for no delay)</param>
        /// <exception cref="InvalidOperationException">Task is already started</exception>
        void Start(TimeSpan firstRunDelay);

        /// <summary>
        /// Start task (and delay first run for specified interval) with cancellation token (instead of <see cref="Stop"/>)
        /// </summary>
        /// <param name="firstRunDelay">Delay before first task run (use TimeSpan.Zero for no delay)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <exception cref="InvalidOperationException">Task is already started</exception>
        void Start(TimeSpan firstRunDelay, CancellationToken cancellationToken);

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