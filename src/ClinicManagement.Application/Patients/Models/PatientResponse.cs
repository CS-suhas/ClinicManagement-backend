namespace ClinicManagement.Application.Patients.Models;

public record PatientResponse(
    Guid Id,
    string Name,
    DateOnly DateOfBirth,
    string PhoneNumber,
    string Email
);