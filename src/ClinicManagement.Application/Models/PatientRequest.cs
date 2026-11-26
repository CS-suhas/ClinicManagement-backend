namespace ClinicManagement.Application.Models;

public record PatientRequest(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email
);