namespace ClinicManagement.Application.Clinics.Models;

public record ClinicResponse(
    Guid Id,
    string Name,
    string? Address,
    string? PhoneNumber
);
