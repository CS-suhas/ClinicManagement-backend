namespace ClinicManagement.Application.Models;

/// <summary>
/// Appointment details optimized for filtered lists and the dashboard.
/// PatientAllergyCount and PatientHighestAllergySeverity describe the patient's recorded allergies (severity is
/// null when the patient has none). CreatedAtUtc is when the appointment was booked. StatusChangedAtUtc is when
/// its status last changed, and falls back to CreatedAtUtc when it never has.
/// </summary>
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
    string Status,
    int PatientAllergyCount,
    string? PatientHighestAllergySeverity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset StatusChangedAtUtc);
