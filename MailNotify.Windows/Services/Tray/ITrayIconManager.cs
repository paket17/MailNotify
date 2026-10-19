namespace MailNotify.Services;

public interface ITrayIconManager : IDisposable
{
    Icon? GetCurrentThemedIcon();
    bool IsTaskbarLight();
    bool IsAppThemeLight();
    void RefreshIcons();
}
