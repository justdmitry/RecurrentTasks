namespace RecurrentTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IRunnable
    {
        Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken);
    }
}
