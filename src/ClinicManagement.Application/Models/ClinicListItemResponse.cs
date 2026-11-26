namespace ClinicManagement.Application.Models;

/// <summary>A clinic in a list or search result, with the number of verified providers working there.</summary>
public record ClinicListItemResponse(
    Guid Id,
    string Name,
    string? Address,
    string? PhoneNumber,
    int ProviderCount
);
