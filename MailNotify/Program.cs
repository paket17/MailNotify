using MailNotify;
using MailNotify.Interfases;
using MailNotify.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddTransient<ExchangeWebService>();
builder.Services.AddTransient<IGetNotifications<ICalendarNotification>, ExchangeCalendarNotify>();
builder.Services.AddTransient<ISendNotifications<ICalendarNotification>, SendUwpNotify>();
builder.Services.AddTransient<NotifyWorker>();
builder.Services.AddTransient<SettingsProvider>();
builder.Services.AddSingleton<INotifyCache, NotifyCache>();

var host = builder.Build();
host.Run();
