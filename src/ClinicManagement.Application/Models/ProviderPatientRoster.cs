namespace ClinicManagement.Application.Models;

/// <summary>A patient who has at least one appointment with a provider, with visits relative to now.</summary>
public sealed record ProviderPatientRosterItem(
    RosterPatient Patient,
    RosterVisit? LastVisit,
    RosterVisit? NextVisit,
    int AppointmentCount);

/// <summary>AllergyCount and HighestSeverity (null when none) summarize the patient's recorded allergies.</summary>
public sealed record RosterPatient(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber,
    int AllergyCount,
    string? HighestSeverity);

public sealed record RosterVisit(
    Guid Id,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit);
