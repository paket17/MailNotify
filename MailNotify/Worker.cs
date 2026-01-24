using MailNotify.Interfaces;

namespace MailNotify;

public class Worker(ILogger<Worker> logger, ISettingsProvider settingsProvider, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Run");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = scopeFactory.CreateScope();
                var notifyWorker = scope.ServiceProvider.GetRequiredService<NotifyWorker>();
                await notifyWorker.Run();
                await Task.Delay(TimeSpan.FromMinutes(settingsProvider.UpdateOffsetMinutes), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error {ex}");
            throw;
        }
    }
}
