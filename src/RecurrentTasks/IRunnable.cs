namespace RecurrentTasks
{
    using System;
    using System.Threading;

    public interface IRunnable
    {
        void Run(ITask currentTask, CancellationToken cancellationToken);
    }
}
