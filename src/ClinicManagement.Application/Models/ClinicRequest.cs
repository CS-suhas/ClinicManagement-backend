namespace ClinicManagement.Application.Models;

public record ClinicRequest(
    Guid Id,
    string Name,
    string? Address,
    string? PhoneNumber = null
);
