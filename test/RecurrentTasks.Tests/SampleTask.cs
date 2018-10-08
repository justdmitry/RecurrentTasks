namespace RecurrentTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class SampleTask : IRunnable
    {
        private SampleTaskSettings settings;

        public SampleTask(SampleTaskSettings settings)
        {
            this.settings = settings;
        }

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (settings.MustSetIntervalToZero)
            {
                currentTask.Options.Interval = TimeSpan.Zero;
            }

            settings.FormatResult = 123456.78.ToString();

            settings.TaskRunCalled.Set();

            if (settings.MustThrowError)
            {
                throw new Exception("You asked - I throw");
            }

            if (settings.MustRunUntilCancelled)
            {
                if (!cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)))
                {
                    throw new Exception("CancellationToken not set during 10 seconds. Something wrong with test...");
                }
            }

            if (!settings.CanContinueRun.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new Exception("CanContinueRun not set during 10 seconds. Something wrong with test...");
            }

            return Task.CompletedTask;
        }
    }
}
