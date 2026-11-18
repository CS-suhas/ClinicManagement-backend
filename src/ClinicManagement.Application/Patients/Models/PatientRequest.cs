namespace ClinicManagement.Application.Patients.Models;

public record PatientRequest(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email
);