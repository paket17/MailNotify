using MailNotify.Interfaces;

namespace MailNotify.Entities;

public record Notification : INotification
{
    public string? Id { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? From { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastUpdate { get; set; }
}
