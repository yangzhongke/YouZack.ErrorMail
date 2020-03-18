﻿namespace YouZack.ErrorMail
{
    /// <summary>
    /// Placeholders of layout：${message}/${logLevel}/${categoryName}/${newline}
    /// /${exception}/${datetime}/${machinename}/${eventId}
    /// </summary>
    public class ErrorMailLoggerOptions
    {
        public string Subject { get; set; } = "Error occured：${message}";
        public string[] To { get; set; }
        public string[] CC { get; set; }
        public string[] Bcc { get; set; }
        public string From { get; set; }
        public string Body { get; set; } = "Time:${datetime}，Machine Name:${machinename}${newline} Message:${message}，Level：${logLevel}，Category Name;${categoryName}${newline}${exception}";
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; } = 25;
        public bool SmtpEnableSsl { get; set; } = true;

        public bool SendSameErrorOnlyOnce { get; set; } = false;
    }
}
