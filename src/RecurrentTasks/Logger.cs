namespace RecurrentTasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
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
                if (options.LogFormatter != null)
                {
                    var logFormat = GetLogFormat<TState>(state);
                    logFormat = options.LogFormatter(logFormat.Format, logFormat.Args);

                    var stateArgs = new object[] {
                        logFormat.Format,
                        logFormat.Args
                    };

                    state = (TState)Activator.CreateInstance(state.GetType(), stateArgs);
                }

                this.logger.Log<TState>(logLevel, eventId, state, exception, formatter);
            }
        }

        private LogFormat GetLogFormat<TState>(TState state)
        {

            var stateFields = state.GetType().GetRuntimeFields();

            var args = (object[])stateFields.FirstOrDefault(o => o.Name == "_values")?
                                            .GetValue(state);

            var originalMessage = (string)stateFields.FirstOrDefault(o => o.Name == "_originalMessage")?
                                                     .GetValue(state);

            var stateFormatter = stateFields.FirstOrDefault(o => o.Name == "_formatter")
                                            .GetValue(state);
  
            if (args == null || args.Length == 0 || stateFormatter == null)
            {
                return new LogFormat(originalMessage, args);
            }
            else
            {
                var formatterProperties = stateFormatter.GetType().GetRuntimeProperties();

                var originalFormat = (string)formatterProperties.FirstOrDefault(o => o.Name == "OriginalFormat")?.GetValue(stateFormatter);

                return new LogFormat(originalFormat, args);
            }
        }
    }
}
