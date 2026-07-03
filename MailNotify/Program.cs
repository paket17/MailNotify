using MailNotify;
using MailNotify.Interfaces;
using MailNotify.Logging;
using MailNotify.Services;

var builder = Host.CreateApplicationBuilder(args);
var appDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(appDirectory, "logs")));
builder.Services.AddMailNotifyServices();

var host = builder.Build();
host.Services.GetRequiredService<IAutoStartService>().Configure();
host.Run();
