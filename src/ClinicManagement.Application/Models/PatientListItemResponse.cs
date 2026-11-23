namespace ClinicManagement.Application.Patients.Models;

/// <summary>A patient in a list, with the size and worst severity of their recorded allergies.</summary>
public record PatientListItemResponse(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email,
    int AllergyCount,
    string? HighestSeverity
);
