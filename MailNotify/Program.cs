using MailNotify.Interfaces;
using MailNotify.Services;
using Serilog;

var appDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = appDirectory
});
var logsDirectory = Path.Combine(appDirectory, "logs");

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(
    new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(
            Path.Combine(logsDirectory, "log-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14)
        .CreateLogger(),
    dispose: true);

builder.Services.AddMailNotifyServices();

var host = builder.Build();
host.Services.GetRequiredService<IAutoStartService>().Configure();
host.Run();
