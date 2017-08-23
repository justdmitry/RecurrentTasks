namespace RecurrentTasks
{
    using System;
    using System.Threading;

    public class SampleTask : IRunnable
    {
        private SampleTaskSettings settings;

        public SampleTask(SampleTaskSettings settings)
        {
            this.settings = settings;
        }

        public void Run(ITask currentTask, CancellationToken cancellationToken)
        {
            if (settings.MustSetIntervalToZero)
            {
                currentTask.Interval = TimeSpan.Zero;
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
        }
    }
}
