namespace RecurrentTasks.Sample
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public class SampleTask : TaskBase<TaskStatus>
    {
        public List<string> Messages { get; } = new List<string>();

        public SampleTask(ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
            : base(loggerFactory, TimeSpan.FromMinutes(1), serviceScopeFactory)
        {
            // Nothing
        }

        protected override void Run(IServiceProvider serviceProvider, TaskStatus state)
        {
            Messages.Add(string.Format("Run at: {0}", DateTimeOffset.Now));
        }
    }
}
