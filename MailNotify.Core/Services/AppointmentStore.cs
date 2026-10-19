using MailNotify.Interfaces;

namespace MailNotify.Services;

public class AppointmentStore
{
    private volatile IReadOnlyCollection<ICalendarNotification> _appointments = [];

    public event Action? AppointmentsChanged;

    public void SetAppointments(IEnumerable<ICalendarNotification> appointments)
    {
        _appointments = [.. appointments];
        AppointmentsChanged?.Invoke();
    }

    public IReadOnlyCollection<ICalendarNotification> GetAppointments() =>
        _appointments;

    public void Clear()
    {
        _appointments = [];
        AppointmentsChanged?.Invoke();
    }
}
