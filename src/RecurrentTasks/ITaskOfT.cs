namespace RecurrentTasks
{
    public interface ITask<TRunnable> : ITask
        where TRunnable : IRunnable
    {
        // Nothing
    }
}
