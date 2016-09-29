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

        public void Run(TaskRunStatus taskRunStatus)
        {
            settings.TaskRunCalled.Set();
            if (settings.MustThrowError)
            {
                throw new Exception("You asked - I throw");
            }
            if (!settings.CanContinueRun.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new Exception("CanContinueRun not set during 10 seconds. Something wrong with test...");
            }
        }
    }
}
