namespace MailNotify.Services;

public interface ITrayMenuPresenter
{
    TrayMenuModel GetMenuModel();
    string GetTooltipText();
    void TriggerSync();
    void OpenUrl(string url);
    void ToggleAutoStart(bool enabled);
    void ExitApplication();
}
