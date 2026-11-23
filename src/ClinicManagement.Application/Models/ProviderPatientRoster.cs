namespace ClinicManagement.Application.Models;

/// <summary>A patient who has at least one appointment with a provider, with visits relative to now.</summary>
public sealed record ProviderPatientRosterItem(
    RosterPatient Patient,
    RosterVisit? LastVisit,
    RosterVisit? NextVisit,
    int AppointmentCount);

public sealed record RosterPatient(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber);

public sealed record RosterVisit(
    Guid Id,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit);
