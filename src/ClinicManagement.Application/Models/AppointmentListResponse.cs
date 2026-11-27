using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public sealed record AppointmentListResponse(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid ProviderId,
    string ProviderName,
    string ProviderSpecialty,
    Guid ClinicId,
    string ClinicName,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit,
    AppointmentStatus Status,
    int PatientAllergyCount,
    AllergySeverity? PatientHighestAllergySeverity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset StatusChangedAtUtc);
