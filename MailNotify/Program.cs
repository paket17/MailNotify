using MailNotify;
using MailNotify.Interfaces;
using MailNotify.Logging;
using MailNotify.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(AppContext.BaseDirectory, "logs")));
builder.Services.AddMailNotifyServices();

var host = builder.Build();
host.Services.GetRequiredService<IAutoStartService>().Configure();
host.Run();
