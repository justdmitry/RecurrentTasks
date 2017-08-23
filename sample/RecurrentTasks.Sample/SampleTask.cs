namespace RecurrentTasks.Sample
{
    using System;
    using System.Threading;
    using Microsoft.Extensions.Logging;

    public class SampleTask : IRunnable
    {
        private ILogger logger;

        private SampleTaskRunHistory runHistory;

        public SampleTask(ILogger<SampleTask> logger, SampleTaskRunHistory runHistory)
        {
            this.logger = logger;
            this.runHistory = runHistory;
        }

        public void Run(ITask currentTask, CancellationToken cancellationToken)
        {
            var msg = string.Format("Run at: {0}", DateTimeOffset.Now);
            runHistory.Messages.Add(msg);
            logger.LogDebug(msg);

            // You can change interval for [all] next runs!
            currentTask.Interval = currentTask.Interval.Add(TimeSpan.FromSeconds(1));
        }
    }
}
