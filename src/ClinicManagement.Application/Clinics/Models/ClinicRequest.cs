namespace ClinicManagement.Application.Clinics.Models;

public record ClinicRequest(
    Guid Id,
    string Name,
    string? Address,
    string PhoneNumber
);
