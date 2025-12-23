using MailNotify.Interfases;

namespace MailNotify.Entities;

public class Notification : INotification
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? From { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastUpdate { get; set; }
}
