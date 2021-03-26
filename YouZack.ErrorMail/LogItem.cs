using Microsoft.Extensions.Logging;
using System;

namespace YouZack.ErrorMail
{
    class LogItem
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public string CategoryName { get; set; }
    }
}
