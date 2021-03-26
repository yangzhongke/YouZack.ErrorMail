using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigurationBuilder configBuilder = new ConfigurationBuilder();
            configBuilder.AddUserSecrets<Program>();
            var config = configBuilder.Build();

            ServiceCollection services = new ServiceCollection();
            services.AddLogging(logBuilder=> {
                logBuilder.AddErrorMail(opt => {
                    opt.From = config["From"];
                    opt.To = new string[] { config["To"] };
                    opt.IntervalSeconds = 10;
                    opt.SmtpEnableSsl = true;
                    opt.SmtpPassword = config["SmtpPassword"];
                    opt.SmtpUserName = config["SmtpUserName"];
                    opt.SmtpServer = config["SmtpServer"];
                });
                logBuilder.AddConsole();
            });
            using(var sp = services.BuildServiceProvider())
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                for (int i = 0;i<10;i++)
                {                    
                    Console.WriteLine("普通1");
                    logger.LogError("日志1");
                    Console.WriteLine("普通2");
                    logger.LogError("日志2");
                    Console.WriteLine("普通3");
                    string s = null;
                    try
                    {
                        s.ToLower();
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex,"error tolower");
                    }
                    Thread.Sleep(100);
                }
            }
        }
    }
}
