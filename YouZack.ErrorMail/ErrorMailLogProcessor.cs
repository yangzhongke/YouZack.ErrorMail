using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace YouZack.ErrorMail
{
    class ErrorMailLogProcessor : IDisposable
    {
        private readonly ConcurrentQueue<LogItem> queue=new ConcurrentQueue<LogItem>();
        private readonly Thread thread;
        private readonly IOptionsSnapshot<ErrorMailLoggerOptions> optConfiguration;

        public ErrorMailLogProcessor(IOptionsSnapshot<ErrorMailLoggerOptions> optConfiguration)
        {
            this.optConfiguration = optConfiguration;
            thread = new Thread(ScanningQueueThread)
            {
                IsBackground = true,
                Name = "ErrorMailLogProcessor Thread"
            };
            thread.Start();
        }

        public void Enqueue(LogItem logItem)
        {
            this.queue.Enqueue(logItem);
        }

        private void ScanningQueueThread()
        {
            var configuration = optConfiguration.Value;
            while (true)
            {
                Thread.Sleep(configuration.IntervalSeconds * 1000);
                List<LogItem> itemsToBeSent = new List<LogItem>();
                while(queue.TryDequeue(out LogItem logItem))
                {
                    itemsToBeSent.Add(logItem);
                }
                if(itemsToBeSent.Count>0)
                {
                    //As for multi messages with the same Key(Message+CategoryName), only the first of them and the count are displayed on the mail.
                    var messages = itemsToBeSent.GroupBy(e => e.Message+" "+e.CategoryName).Select(g=>new { Key=g.Key,
                        Preview= g.First(),Count = g.Count()});
                    StringBuilder sbBody = new StringBuilder();
                    sbBody.AppendLine("<ul>");
                    foreach(var msg in messages)
                    {
                        sbBody.AppendLine("<ol>");
                        sbBody.Append(msg.Key).Append("(Count: ")
                            .Append(msg.Count).AppendLine(")");

                        string body = FormatLayout(configuration.Body, 
                                    msg.Preview.Message,msg.Preview.Exception,
                                    msg.Preview.CategoryName,
                                    msg.Preview.LogLevel, msg.Preview.EventId);
                        sbBody.AppendLine("<pre>").AppendLine(body).AppendLine("</pre>");
                        sbBody.AppendLine("</ol>");
                    }
                    sbBody.AppendLine("</ul>");
                    string subject = configuration.Subject;
                    SendMail(subject, sbBody.ToString());
                }                
            }            
        }

        static void AddRange(MailAddressCollection collection, string[] addresses)
        {
            if (addresses == null)
            {
                return;
            }
            foreach (string address in addresses)
            {
                collection.Add(address);
            }
        }

        private static string FormatLayout(string template, string message, 
            Exception exception,string categoryName, LogLevel logLevel, EventId eventId)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();
            replacements["{{message}}"] = message;
            if (exception == null)
            {
                replacements["{{exception}}"] = "";
            }
            else
            {
                replacements["{{exception}}"] = exception.ToString();
            }
            replacements["{{categoryName}}"] = categoryName;
            replacements["{{logLevel}}"] = logLevel.ToString();
            replacements["{{newline}}"] = Environment.NewLine.ToString();
            replacements["{{datetime}}"] = DateTime.Now.ToString();
            replacements["{{machinename}}"] = Environment.MachineName;
            replacements["{{eventId}}"] = eventId.ToString();            

            Regex regex = new Regex("({{message}})|({{exception}})|({{logLevel}})|({{categoryName}})|({{newline}})|({{datetime}})|({{machinename}})|({{eventId}})");
            return regex.Replace(template, 
                me=>replacements.ContainsKey(me.Value)? replacements[me.Value]: me.Value);
        }

        private void SendMail(string subject, string body)
        {
            /*
            File.AppendAllText("d:/1.txt",DateTime.Now.ToString()+""+body);
            File.AppendAllText("d:/1.txt", "\r\n****************\r\n");
            return;*/
            var configuration = optConfiguration.Value;
            using (var mailMessage = new MailMessage())
            using (var client = new SmtpClient())
            {
                AddRange(mailMessage.To, configuration.To);
                AddRange(mailMessage.CC, configuration.CC);
                AddRange(mailMessage.Bcc, configuration.Bcc);
                mailMessage.From = new MailAddress(configuration.From);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;
                client.Host = configuration.SmtpServer;
                if(configuration.SmtpPort!=null)
                {
                    client.Port = configuration.SmtpPort.Value;
                }                
                client.EnableSsl = configuration.SmtpEnableSsl;
                client.Credentials = new NetworkCredential(configuration.SmtpUserName, configuration.SmtpPassword);
                client.Send(mailMessage);
            }
        }

        public void Dispose()
        {
            try
            {
                thread.Join(10*1000);
            }
            catch (ThreadStateException) { }
        }
    }
}
