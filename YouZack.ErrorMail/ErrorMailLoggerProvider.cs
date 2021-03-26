using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace YouZack.ErrorMail
{
    public class ErrorMailLoggerProvider : ILoggerProvider
    {
        private readonly IOptionsSnapshot<ErrorMailLoggerOptions> options;
        private readonly ErrorMailLogProcessor processor;
        public ErrorMailLoggerProvider(IOptionsSnapshot<ErrorMailLoggerOptions> options)
        {
            this.options = options;
            this.processor = new ErrorMailLogProcessor(options);
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new ErrorMailLogger(categoryName, processor);
        }

        public void Dispose()
        {
            this.processor.Dispose();
        }
    }
}
