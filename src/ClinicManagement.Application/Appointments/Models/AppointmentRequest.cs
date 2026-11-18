namespace ClinicManagement.Application.Appointments.Models;

public record AppointmentRequest(
    Guid Id,
    Guid PatientId,
    Guid ProviderId,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit
);
