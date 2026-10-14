using MailNotify.Interfaces;
using MailNotify.Services;
using NSubstitute;

namespace MailNotify.Tests;

public class AppointmentStoreTests
{
    [Fact]
    public void GetAppointments_ReturnsEmptyCollection_Initially()
    {
        var store = new AppointmentStore();

        var appointments = store.GetAppointments();

        appointments.Should().BeEmpty();
    }

    [Fact]
    public void SetAppointments_StoresAppointmentsAndReturnsSnapshot()
    {
        var store = new AppointmentStore();
        var item1 = Substitute.For<ICalendarNotification>();
        var item2 = Substitute.For<ICalendarNotification>();
        var inputList = new List<ICalendarNotification> { item1, item2 };

        store.SetAppointments(inputList);

        var result = store.GetAppointments();
        result.Should().HaveCount(2).And.ContainInOrder(item1, item2);

        inputList.Clear();
        store.GetAppointments().Should().HaveCount(2);
    }

    [Fact]
    public void Clear_EmptiesTheStore()
    {
        var store = new AppointmentStore();
        var item = Substitute.For<ICalendarNotification>();
        store.SetAppointments([item]);

        store.Clear();

        store.GetAppointments().Should().BeEmpty();
    }

    [Fact]
    public async Task ConcurrentAccess_RemainsThreadSafe()
    {
        var store = new AppointmentStore();
        var item = Substitute.For<ICalendarNotification>();

        var writeTask = Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
                store.SetAppointments([item]);
        });

        var readTask = Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
                _ = store.GetAppointments();
        });

        await Task.WhenAll(writeTask, readTask);
        store.GetAppointments().Should().ContainSingle();
    }
}
