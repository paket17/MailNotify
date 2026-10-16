using MailNotify.Interfaces;

namespace MailNotify.Services;

public class AppointmentStore
{
    private volatile IReadOnlyCollection<ICalendarNotification> _appointments = [];

    public void SetAppointments(IEnumerable<ICalendarNotification> appointments) =>
        _appointments = [.. appointments];

    public IReadOnlyCollection<ICalendarNotification> GetAppointments() =>
        _appointments;

    public void Clear() =>
        _appointments = [];
}
