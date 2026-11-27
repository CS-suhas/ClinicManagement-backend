using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid ProviderId,
    string ProviderName,
    string ProviderSpecialty,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit,
    AppointmentStatus Status
);

