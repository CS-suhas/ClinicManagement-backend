namespace ClinicManagement.Application.Appointments.Models;

public record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    Guid ProviderId,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit
);
