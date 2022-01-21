namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Globalization;
    using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Sets <see cref="TaskOptions.Logger"/> (custom logger to use instead of calling loggerFactory.CreateLogger()).
        /// </summary>
        public static TaskOptions WithLogger(this TaskOptions taskOptions, ILogger logger)
        {
            taskOptions.Logger = logger;
            return taskOptions;
        }

        /// <summary>
        /// Sets <see cref="TaskOptions.RunCulture"/>, that will be set before <see cref="IRunnable.RunAsync"/> is called.
        /// </summary>
        public static TaskOptions WithCulture(this TaskOptions taskOptions, CultureInfo culture)
        {
            taskOptions.RunCulture = culture;
            return taskOptions;
        }
    }
}
