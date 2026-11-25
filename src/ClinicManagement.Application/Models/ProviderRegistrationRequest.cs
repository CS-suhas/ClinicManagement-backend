namespace ClinicManagement.Application.Providers.Models;

public record ProviderRegistrationRequest(
    Guid Id,
    Guid ClinicId,
    string FirstName,
    string LastName,
    string Specialty,
    decimal? Fee = null,
    string? Gender = null
);
