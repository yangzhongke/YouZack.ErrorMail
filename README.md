# YouZack.ErrorMail
Mail Logging provider on .Net Core, which can be configured as 'send the same error only once', if SendSameErrorOnlyOnce=true, if there are more than one same messages ocurred, only one mail would be sent in the time of "IntervalSeconds" senconds.
```
Install-Package YouZack.ErrorMail
```

```C#
services.AddLogging(builder => {
	builder.AddErrorMail(opt => {
		opt.From = "server@xxx.com";
		opt.To = new []{"yzk@xxx.com" };
		opt.IntervalSeconds = 30;
		opt.SendSameErrorOnlyOnce = true;
		opt.SmtpEnableSsl = true;
		opt.SmtpUserName = "server@xxxx.com";
		opt.SmtpPassword = "2323423fasfadsaf";
		opt.SmtpServer = "smtp.xxx.com";
		opt.SmtpEnableSsl = true;
	});
});
```



