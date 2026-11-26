namespace ClinicManagement.Application.Models;

public record PatientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email
);