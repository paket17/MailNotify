using MailNotify.Interfaces;

namespace MailNotify;

public class Worker(ILogger<Worker> logger, ISettingsProvider settingsProvider, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const int ReminderCheckIntervalSeconds = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Run");
        var getAppointmentsTask = Task.Run(() => RunGetAppointmentsLoopAsync(stoppingToken), stoppingToken);
        var reminderTask = Task.Run(() => RunReminderLoopAsync(stoppingToken), stoppingToken);

        await Task.WhenAll(getAppointmentsTask, reminderTask);
    }

    private async Task RunGetAppointmentsLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var notifyWorker = scope.ServiceProvider.GetRequiredService<NotifyWorker>();
                notifyWorker.GetAppointments(stoppingToken);
                notifyWorker.CheckReminders(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {}
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in NotifyWorker getting appointments");
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

    private async Task RunReminderLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(ReminderCheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var notifyWorker = scope.ServiceProvider.GetRequiredService<NotifyWorker>();
                notifyWorker.CheckReminders(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {}
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in NotifyWorker reminders");
            }
        }
    }
}
