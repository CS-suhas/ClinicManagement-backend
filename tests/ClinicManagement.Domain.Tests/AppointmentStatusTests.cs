using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Tests;

public class AppointmentStatusTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static Appointment NewAppointment() =>
        Appointment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Now.AddDays(1),
            "Checkup",
            Now);

    [Fact]
    public void NewAppointment_StartsAsRequested()
    {
        Assert.Equal(AppointmentStatus.REQUESTED, NewAppointment().Status);
    }

    [Fact]
    public void Confirm_And_Decline_AreOnlyAllowedFromRequested()
    {
        var confirmed = NewAppointment();
        confirmed.Confirm(Now);
        var declined = NewAppointment();
        declined.Decline(Now);

        Assert.Equal(AppointmentStatus.CONFIRMED, confirmed.Status);
        Assert.Equal(AppointmentStatus.DECLINED, declined.Status);
        Assert.Throws<InvalidStatusTransitionException>(() => confirmed.Confirm(Now));
        Assert.Throws<InvalidStatusTransitionException>(() => confirmed.Decline(Now));
        Assert.Throws<InvalidStatusTransitionException>(() => declined.Confirm(Now));
    }

    [Fact]
    public void Cancel_IsAllowedFromRequestedAndConfirmed_ButNotFromFinalStates()
    {
        var requested = NewAppointment();
        var confirmed = NewAppointment();
        confirmed.Confirm(Now);
        var declined = NewAppointment();
        declined.Decline(Now);

        requested.Cancel(Now);
        confirmed.Cancel(Now);

        Assert.Equal(AppointmentStatus.CANCELLED, requested.Status);
        Assert.Equal(AppointmentStatus.CANCELLED, confirmed.Status);
        Assert.Throws<InvalidStatusTransitionException>(() => declined.Cancel(Now));
        Assert.Throws<InvalidStatusTransitionException>(() => requested.Cancel(Now));
    }

    [Fact]
    public void Complete_RequiresAConfirmedAppointment()
    {
        var requested = NewAppointment();
        var confirmed = NewAppointment();
        confirmed.Confirm(Now);

        confirmed.Complete(Now);

        Assert.Equal(AppointmentStatus.COMPLETED, confirmed.Status);
        Assert.Throws<InvalidStatusTransitionException>(() => requested.Complete(Now));
        Assert.Throws<InvalidStatusTransitionException>(() => confirmed.Cancel(Now));
    }
}
