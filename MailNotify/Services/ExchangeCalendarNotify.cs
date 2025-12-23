using MailNotify.Entities;
using MailNotify.Interfases;
using Microsoft.Exchange.WebServices.Data;

namespace MailNotify.Services;

public class ExchangeCalendarNotify : IGetNotifications<ICalendarNotification>
{
    private readonly TimeSpan reminderOffset;
    private readonly DateTime start;
    private readonly DateTime end;
    private readonly CalendarView calendarView;
    private readonly ExchangeService exchangeService;

    public IEnumerable<ICalendarNotification> GetNotifications()
    {
        return exchangeService.FindAppointments(WellKnownFolderName.Calendar, calendarView)
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
            })
            .Where(i => DateTime.Now >= i.Start - reminderOffset && DateTime.Now < i.Start)
            .ToArray();
    }

    public ExchangeCalendarNotify(ExchangeWebService exchangeService, SettingsProvider settingsProvider)
    {
        this.exchangeService = exchangeService.GetExchangeService();
        reminderOffset = TimeSpan.FromMinutes(settingsProvider.ReminderOffsetMinutes);
        start = DateTime.Today;
        end = DateTime.Today.AddDays(1).AddSeconds(-1);
        calendarView = new(start, end)
        {
            PropertySet = new(ItemSchema.Id,
                              ItemSchema.Subject,
                              AppointmentSchema.Start,
                              AppointmentSchema.Duration,
                              AppointmentSchema.Location,
                              ItemSchema.WebClientReadFormQueryString,
                              ItemSchema.LastModifiedTime)
        };
    }
}
