using MailNotify;
using MailNotify.Interfaces;
using MailNotify.Logging;
using MailNotify.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(AppContext.BaseDirectory, "logs")));
builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<ExchangeWebService>();
builder.Services.AddScoped<IGetNotifications<ICalendarNotification>, ExchangeCalendarNotify>();
builder.Services.AddScoped<ISendNotifications<ICalendarNotification>, SendUwpNotify>();
builder.Services.AddScoped<NotifyWorker>();
builder.Services.AddSingleton<IAutoStartService, WindowsAutoStartService>();
builder.Services.AddSingleton<ISettingsProvider, SettingsProvider>();
builder.Services.AddSingleton<INotifyCache, NotifyCache>();

var host = builder.Build();
host.Services.GetRequiredService<IAutoStartService>().Configure();
host.Run();
