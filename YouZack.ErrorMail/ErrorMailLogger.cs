using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace YouZack.ErrorMail
{
    public class ErrorMailLogger : ILogger
    {
        private readonly string categoryName;
        //因为ErrorMailLogger是单例，所以不能用IOptionsSnapshot，因为IOptionsSnapshot是Scoped的（每次Scoped刷新一次)
        private readonly IOptions<ErrorMailLoggerOptions> options;
        private readonly ConcurrentDictionary<string,DateTime> sendedMessagesCache 
            = new ConcurrentDictionary<string, DateTime>();

        public ErrorMailLogger(string categoryName, IOptions<ErrorMailLoggerOptions> options)
        {
            this.categoryName = categoryName;
            this.options = options;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error || logLevel == LogLevel.Critical;
        }

        private void CleanExpiredMessagesCache()
        {
            int intervalSec = options.Value.IntervalSeconds;
            var expiredDateTime = DateTime.Now.AddSeconds(-intervalSec);
            List<string> keysToBeRemoved = new List<string>();
            foreach (var kv in this.sendedMessagesCache)
            {
                if (kv.Value < expiredDateTime)
                {
                    keysToBeRemoved.Add(kv.Key);
                }
            }
            foreach(var key in keysToBeRemoved)
            {
                this.sendedMessagesCache.TryRemove(key,out DateTime dt);               
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
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
            CleanExpiredMessagesCache();
            var opt = options.Value;
            if (opt.SendSameErrorOnlyOnce&& sendedMessagesCache.ContainsKey(message))
            {
                return;
            }

            string body = FormatLayout(opt.Body, message, categoryName, logLevel, eventId, exception);
            string subject = FormatLayout(opt.Subject, message, categoryName, logLevel, eventId, exception);
            SendMail(subject, body);
            sendedMessagesCache[message] = DateTime.Now;
        }

        static void AddRange(MailAddressCollection collection,string[] addresses)
        {
            if(addresses==null)
            {
                return;
            }
            foreach (string address in addresses)
            {
                collection.Add(address);
            }
        }

        private void SendMail(string subject, string body)
        {
            var opt = options.Value;
            using (var mailMessage = new MailMessage())
            using (var client = new SmtpClient())
            {
                AddRange(mailMessage.To, opt.To);
                AddRange(mailMessage.CC, opt.CC);
                AddRange(mailMessage.Bcc, opt.Bcc);
                mailMessage.From = new MailAddress(opt.From);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = false;
                client.Host = opt.SmtpServer;
                client.Port = opt.SmtpPort;
                client.EnableSsl = opt.SmtpEnableSsl;
                client.Credentials = new NetworkCredential(opt.SmtpUserName, opt.SmtpPassword);
                client.Send(mailMessage);
            }
        }

        private static string FormatLayout(string template, string message,string categoryName,
            LogLevel logLevel, EventId eventId, Exception exception)
        {
            string result = template.Replace("${message}", message).Replace("${logLevel}", logLevel.ToString())
                .Replace("${categoryName}", categoryName).Replace("${newline}", Environment.NewLine);
            if (exception != null)
            {
                result = result.Replace("${exception}", exception.StackTrace);
            }
            result = result.Replace("${datetime}", DateTime.Now.ToString())
                .Replace("${machinename}", Environment.MachineName)
                .Replace("${eventId}", eventId.ToString());
            return result;
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
