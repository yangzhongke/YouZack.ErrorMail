using Microsoft.Extensions.Logging;
using System;

namespace YouZack.ErrorMail
{
    class ErrorMailLogger : ILogger
    {
        private readonly ErrorMailLogProcessor processor;
        private readonly string categoryName;
        public ErrorMailLogger(string categoryName,ErrorMailLogProcessor processor)
        {
            this.categoryName = categoryName;
            this.processor = processor;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Warning || logLevel == LogLevel.Error
                || logLevel == LogLevel.Critical;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //because the "Log" method blocks the caller method, so if the given logLevel is not enabled, the method returns directly to keep performant
            if(!IsEnabled(logLevel))
            {
                return;
            }
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            //https://stackoverflow.com/questions/59919244/how-should-async-logging-to-database-be-implemented-in-asp-net-core-application
            //messages should be put into queue to avoid low performance
            this.processor.Enqueue(new LogItem {EventId=eventId,Message=message,LogLevel=logLevel,Exception=exception,CategoryName=categoryName });
        }

        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }
}
