namespace MailNotify.Interfaces;

public interface IAutoStartService
{
    void Configure();
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
}
