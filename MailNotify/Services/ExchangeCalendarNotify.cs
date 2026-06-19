using MailNotify.Entities;
using MailNotify.Interfaces;
using Microsoft.Exchange.WebServices.Data;

namespace MailNotify.Services;

public class ExchangeCalendarNotify(ExchangeService exchangeService) : IGetNotifications<ICalendarNotification>
{
    public IEnumerable<ICalendarNotification> GetNotifications(DateTime start, DateTime end)
    {
        var calendarView = GetCalendarView(start, end);
        return [.. exchangeService.FindAppointments(WellKnownFolderName.Calendar, calendarView)
            .Select(i =>
            {
                return new CalendarNotification()
                {
                    Id = i.Id.UniqueId,
                    Subject = i.Subject,
                    Location = i.Location,
                    Start = i.Start,
                    Duration = i.Duration,
                    WebUrl = i.WebClientReadFormQueryString,
                    LastUpdate = i.LastModifiedTime
                };
            })];
    }

    private CalendarView GetCalendarView(DateTime start, DateTime end)
    {
        return new CalendarView(start, end)
        {
            PropertySet = new(
                ItemSchema.Id,
                ItemSchema.Subject,
                AppointmentSchema.Start,
                AppointmentSchema.Duration,
                AppointmentSchema.Location,
                ItemSchema.WebClientReadFormQueryString,
                ItemSchema.LastModifiedTime)
        };
    }
}
