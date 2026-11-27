using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

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
    string PhoneNumber,
    int AllergyCount,
    AllergySeverity? HighestSeverity);

public sealed record RosterVisit(
    Guid Id,
    DateTimeOffset AppointmentDateTime,
    string? ReasonForVisit);
