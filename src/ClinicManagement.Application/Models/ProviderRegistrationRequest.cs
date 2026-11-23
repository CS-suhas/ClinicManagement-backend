namespace ClinicManagement.Application.Providers.Models;

public record ProviderRegistrationRequest(
    Guid Id,
    string FirstName,
    string LastName,
    string Specialty,
    decimal? Fee = null,
    string? Gender = null
);
