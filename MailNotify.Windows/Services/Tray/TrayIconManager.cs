namespace MailNotify.Services;

public class TrayIconManager : ITrayIconManager
{
    private readonly IWindowsThemeService themeService;
    private Icon? baseIcon;
    private Icon? darkTaskbarIcon;
    private Icon? lightTaskbarIcon;

    public TrayIconManager() : this(new WindowsThemeService())
    {
    }

    public TrayIconManager(IWindowsThemeService themeService)
    {
        this.themeService = themeService;
        RefreshIcons();
    }

    public void RefreshIcons()
    {
        DisposeIcons();

        darkTaskbarIcon = LoadIconFile("notification_icon_dark.ico");
        lightTaskbarIcon = LoadIconFile("notification_icon_light.ico");
        baseIcon = LoadIconFile("notification_icon.ico") ?? GetApplicationIcon();
    }

    public Icon? GetCurrentThemedIcon()
    {
        var isTaskbarDark = themeService.IsTaskbarDark();
        var themed = isTaskbarDark ? darkTaskbarIcon : lightTaskbarIcon;
        return themed ?? baseIcon;
    }

    public bool IsTaskbarLight() => !themeService.IsTaskbarDark();

    public bool IsAppThemeLight() => !themeService.IsDarkTheme();

    private static Icon? LoadIconFile(string fileName)
    {
        try
        {
            var appDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            var fullPath = Path.Combine(appDir, fileName);
            if (File.Exists(fullPath))
                return new Icon(fullPath);

            var baseDir = Path.Combine(AppContext.BaseDirectory, fileName);
            if (File.Exists(baseDir))
                return new Icon(baseDir);
        }
        catch { }

        return null;
    }

    private static Icon GetApplicationIcon()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon is not null)
                    return icon;
            }

            var iconPath = Path.Combine(AppContext.BaseDirectory, "notification_icon.ico");
            if (File.Exists(iconPath))
                return new Icon(iconPath);
        }
        catch { }

        return SystemIcons.Application;
    }

    private void DisposeIcons()
    {
        darkTaskbarIcon?.Dispose();
        darkTaskbarIcon = null;

        lightTaskbarIcon?.Dispose();
        lightTaskbarIcon = null;

        if (baseIcon != SystemIcons.Application)
            baseIcon?.Dispose();

        baseIcon = null;
    }

    public void Dispose()
    {
        DisposeIcons();
        GC.SuppressFinalize(this);
    }
}
