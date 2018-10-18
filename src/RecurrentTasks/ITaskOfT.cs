namespace RecurrentTasks
{
    using System;

    public interface ITask<TRunnable> : ITask
        where TRunnable : IRunnable
    {
        /// <summary>
        /// Type of <see cref="TRunnable"/> this task use (for easier retrieval for logging etc)
        /// </summary>
        Type RunnableType { get; }
    }
}
