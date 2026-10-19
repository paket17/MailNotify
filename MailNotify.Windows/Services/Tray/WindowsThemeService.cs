using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MailNotify.Services;

public class WindowsThemeService : IWindowsThemeService
{
    public bool IsDarkTheme()
    {
        // Native .NET 9+ WinForms dark mode detection
        if (Application.IsDarkModeEnabled)
            return true;

        // Universal fallback for Windows 7 / 8 / High Contrast
        return SystemColors.Window.GetBrightness() < 0.5f;
    }

    public bool IsTaskbarDark()
    {
        // В Windows 10/11 пользователь может настроить «Особый» (Custom) режим оформления:
        // тему приложений сделать светлой (AppsUseLightTheme = 1), а тему панели задач — тёмной (SystemUsesLightTheme = 0).
        // В .NET и Windows API нет публичного метода для чтения темы оболочки Taskbar (Explorer),
        // поэтому чтение ключа SystemUsesLightTheme является общепринятым стандартом Windows.
        // Защита IsWindowsVersionAtLeast(10) гарантирует, что на Windows 7/8 обращение к этому ключу не выполняется.
        if (OperatingSystem.IsWindowsVersionAtLeast(10))
        {
            try
            {
                var value = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "SystemUsesLightTheme",
                    null);

                if (value is int intVal)
                    return intVal == 0;
            }
            catch { }
        }

        // Если ключ недоступен или версия ОС ниже Windows 10, используем тему приложений
        return IsDarkTheme();
    }

    public ThemeColors GetColors() => IsDarkTheme() ? ThemeColors.Dark : ThemeColors.Light;
}
