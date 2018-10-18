namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using RecurrentTasks;

    public static class TaskOptionsExtensions
    {
        public static TaskOptions AutoStart(this TaskOptions taskOptions, TimeSpan interval)
        {
            taskOptions.Interval = interval;
            return taskOptions;
        }

        public static TaskOptions AutoStart(this TaskOptions taskOptions, uint interval)
        {
            return AutoStart(taskOptions, TimeSpan.FromSeconds(interval));
        }

        public static TaskOptions AutoStart(this TaskOptions taskOptions, TimeSpan interval, TimeSpan firstRunDelay)
        {
            taskOptions.Interval = interval;
            taskOptions.FirstRunDelay = firstRunDelay;
            return taskOptions;
        }

        public static TaskOptions AutoStart(this TaskOptions taskOptions, uint interval, uint firstRunDelay)
        {
            return AutoStart(taskOptions, TimeSpan.FromSeconds(interval), TimeSpan.FromSeconds(firstRunDelay));
        }
    }
}
