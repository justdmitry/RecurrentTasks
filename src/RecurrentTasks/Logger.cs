namespace RecurrentTasks
{
    using System;
    using Microsoft.Extensions.Logging;

    internal class Logger : ILogger
    {
        private readonly ILogger logger;

        private readonly TaskOptions options;

        public Logger(ILogger logger, TaskOptions options)
        {
            this.logger = logger;
            this.options = options;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this.logger.BeginScope<TState>(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return this.logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (options.DisableLogger)
            {
                // Do nothing
            }
            else
            {
                this.logger.Log<TState>(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}
