namespace ClinicManagement.Application.Providers.Models;

public record ProviderRequest(
    Guid Id,
    Guid ClinicId,
    string FirstName,
    string LastName,
    string Specialty
);
