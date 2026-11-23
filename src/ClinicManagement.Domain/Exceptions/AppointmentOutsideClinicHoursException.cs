using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Domain.Exceptions;

public sealed class AppointmentOutsideClinicHoursException(
    TimeOnly defaultClinicStartTime,
    TimeOnly defaultClinicEndTime)
    : DomainValidationException(
        $"Appointment must start at or after {defaultClinicStartTime} and end at or before {defaultClinicEndTime}.",
        nameof(Appointment.AppointmentDateTime))
{
    public TimeOnly DefaultClinicStartTime { get; } = defaultClinicStartTime;
    public TimeOnly DefaultClinicEndTime { get; } = defaultClinicEndTime;
}
