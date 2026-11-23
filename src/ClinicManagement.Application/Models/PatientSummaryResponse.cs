namespace ClinicManagement.Application.Patients.Models;

/// <summary>
/// A patient with their allergies and the visits the caller may see. Visit lists are scoped by the caller's role;
/// see PatientService.GetPatientSummaryAsync.
/// </summary>
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

public sealed record PatientSummaryAllergy(Guid AllergyId, string Name, string Severity);

/// <summary>An appointment as shown on a patient summary. Status is the stored status name.</summary>
public sealed record PatientVisitResponse(
    Guid Id,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit,
    string Status,
    string ProviderName);

/// <summary>Appointment counts by status over the appointments the caller may see.</summary>
public sealed record PatientVisitCounts(int Total, int Requested, int Confirmed, int Completed, int Cancelled, int Declined);
