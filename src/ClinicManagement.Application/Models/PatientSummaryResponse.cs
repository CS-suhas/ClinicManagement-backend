using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public sealed record PatientSummaryResponse(
    PatientSummaryPatient Patient,
    IReadOnlyList<PatientSummaryAllergy> Allergies,
    PatientVisitResponse? LastVisit,
    PatientVisitResponse? NextVisit,
    PatientVisitCounts Counts,
    IReadOnlyList<PatientVisitResponse> Recent);

public sealed record PatientSummaryPatient(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber);

public sealed record PatientSummaryAllergy(Guid AllergyId, string Name, AllergySeverity Severity);

public sealed record PatientVisitResponse(
    Guid Id,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit,
    AppointmentStatus Status,
    string ProviderName);

public sealed record PatientVisitCounts(int Total, int Requested, int Confirmed, int Completed, int Cancelled, int Declined);
