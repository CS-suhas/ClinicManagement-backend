namespace ClinicManagement.Application.Models;

public record ClinicListItemResponse(
    Guid Id,
    string Name,
    string? Address,
    string? PhoneNumber,
    int ProviderCount
);
