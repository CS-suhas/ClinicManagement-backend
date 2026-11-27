using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public record PatientListItemResponse(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    int AllergyCount,
    AllergySeverity? HighestSeverity
);
