using MailNotify.Interfaces;

namespace MailNotify;

public class Worker(ILogger<Worker> logger, ISettingsProvider settingsProvider, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Run");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notifyWorker = scope.ServiceProvider.GetRequiredService<NotifyWorker>();
                await notifyWorker.Run(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {}
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in NotifyWorker");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(settingsProvider.UpdateOffsetMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
