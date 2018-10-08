namespace RecurrentTasks.Sample
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
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

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var msg = string.Format("Run at: {0}", DateTimeOffset.Now);
            runHistory.Messages.Add(msg);
            logger.LogDebug(msg);

            // You can change interval for [all] next runs!
            currentTask.Options.Interval = currentTask.Options.Interval.Add(TimeSpan.FromSeconds(1));
            return Task.CompletedTask;
        }
    }
}
