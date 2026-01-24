using MailNotify;
using MailNotify.Interfaces;
using MailNotify.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddScoped<ExchangeWebService>();
builder.Services.AddScoped<IGetNotifications<ICalendarNotification>, ExchangeCalendarNotify>();
builder.Services.AddScoped<ISendNotifications<ICalendarNotification>, SendUwpNotify>();
builder.Services.AddScoped<NotifyWorker>();
builder.Services.AddSingleton<ISettingsProvider, SettingsProvider>();
builder.Services.AddSingleton<INotifyCache, NotifyCache>();

var host = builder.Build();
host.Run();
