namespace ClinicManagement.Application.Providers.Models;

public record ProviderRegistrationRequest(
    Guid Id,
    string FirstName,
    string LastName,
    string Specialty
);
