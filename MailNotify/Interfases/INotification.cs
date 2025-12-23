namespace MailNotify.Interfases;

public interface INotification
{
    string? Id { get; set; }
    string? Subject { get; set; }
    string? Message { get; set; }
    string? From { get; set; }
    DateTime Created { get; set; }
    DateTime LastUpdate { get; set; }
}
