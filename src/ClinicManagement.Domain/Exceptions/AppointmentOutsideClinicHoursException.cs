using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Domain.Exceptions;

public sealed class AppointmentOutsideClinicHoursException(
    TimeOnly defaultClinicStartTime,
    TimeOnly defaultClinicEndTime)
    : DomainValidationException(
        $"Appointment must start after {defaultClinicStartTime} and end before {defaultClinicEndTime}.",
        nameof(Appointment.AppointmentDateTime))
{
    public TimeOnly DefaultClinicStartTime { get; } = defaultClinicStartTime;
    public TimeOnly DefaultClinicEndTime { get; } = defaultClinicEndTime;
}
