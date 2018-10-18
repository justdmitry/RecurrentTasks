namespace RecurrentTasks
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Hosting;

    public interface ITask : IHostedService
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
        /// Information about task result (last run time, last exception, etc)
        /// </summary>
        TaskRunStatus RunStatus { get; }

        /// <summary>
        /// Task options
        /// </summary>
        /// <remarks>All properties may be changed while running (take effect on next run)</remarks>
        TaskOptions Options { get; }

        /// <summary>
        /// Type of <see cref="IRunnable"/> this task use (for easier retrieval for logging etc)
        /// </summary>
        Type RunnableType { get; }

        /// <summary>
        /// Start task with cancellation token (instead of <see cref="Stop"/>)
        /// </summary>
        /// <exception cref="InvalidOperationException">Task is already started</exception>
        void Start();

        /// <summary>
        /// Start task with cancellation token (instead of <see cref="Stop"/>)
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <exception cref="InvalidOperationException">Task is already started</exception>
        void Start(CancellationToken cancellationToken);

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