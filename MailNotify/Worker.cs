namespace MailNotify;

public class Worker(ILogger<Worker> logger, NotifyWorker notifyWorker) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Run");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await notifyWorker.Run();
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error {ex}");
            throw;
        }
    }
}
