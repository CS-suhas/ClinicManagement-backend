namespace ClinicManagement.Application.Providers.Models;

public record ProviderResponse(
    Guid Id,
    Guid ClinicId,
    string Name,
    string Specialty
);
