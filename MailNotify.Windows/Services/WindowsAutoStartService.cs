using MailNotify.Interfaces;
using Microsoft.Win32;

namespace MailNotify.Services;

public class WindowsAutoStartService(ILogger<WindowsAutoStartService> logger, ISettingsProvider settingsProvider) 
    : IAutoStartService
{
    private const string RunRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MailNotify";

    public bool IsEnabled
    {
        get
        {
            try
            {
                using var registryKey = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: false);
                return registryKey?.GetValue(AppName) is not null || registryKey?.GetValue(AppDomain.CurrentDomain.FriendlyName) is not null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check autostart status");
                return false;
            }
        }
    }

    public void Configure() => SetEnabled(settingsProvider.AutoStart);

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(RunRegistryKeyPath, writable: true);
            if (registryKey is null)
            {
                logger.LogWarning("Run registry key was not found");
                return;
            }

            if (enabled)
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

        registryKey.SetValue(AppName, $"\"{Environment.ProcessPath}\"");
        logger.LogInformation("Autostart enabled");
    }

    private void TurnOff(RegistryKey registryKey)
    {
        if (registryKey.GetValue(AppName) is not null)
        {
            registryKey.DeleteValue(AppName, throwOnMissingValue: false);
            logger.LogInformation("Autostart disabled");
        }

        if (registryKey.GetValue("MailNotify.Windows") is not null)
            registryKey.DeleteValue("MailNotify.Windows", throwOnMissingValue: false);
    }
}
