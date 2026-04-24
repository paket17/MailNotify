using MailNotify.Interfaces;
using Microsoft.Win32;

namespace MailNotify.Services;

public class WindowsAutoStartService(ILogger<WindowsAutoStartService> logger, ISettingsProvider settingsProvider) 
    : IAutoStartService
{
    private const string RunRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public void Configure()
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: true);
            if (registryKey is null)
            {
                logger.LogWarning("Run registry key was not found");
                return;
            }

            if (settingsProvider.AutoStart)
                TurnOn(registryKey);
            else
                TurnOff(registryKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure autostart");
        }
    }

    private void TurnOn(RegistryKey registryKey)
    {
        if (string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            logger.LogWarning("Process path is empty, autostart was not configured");
            return;
        }

        registryKey.SetValue(AppDomain.CurrentDomain.FriendlyName, $"\"{Environment.ProcessPath}\"");
        logger.LogInformation("Autostart enabled");
    }

    private void TurnOff(RegistryKey registryKey)
    {
        if (registryKey.GetValue(AppDomain.CurrentDomain.FriendlyName) is not null)
        {
            registryKey.DeleteValue(AppDomain.CurrentDomain.FriendlyName, throwOnMissingValue: false);
            logger.LogInformation("Autostart disabled");
        }
    }
}
