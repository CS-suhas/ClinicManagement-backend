namespace ClinicManagement.Application.Models;

/// <summary>Appointment details optimized for filtered lists and the dashboard.</summary>
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
    string Status);
