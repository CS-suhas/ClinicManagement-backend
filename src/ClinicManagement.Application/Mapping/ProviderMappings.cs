using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Providers.Mapping;

public static class ProviderMappings
{
    public static Provider ToEntity(this ProviderRequest request) =>
        Provider.Create(
            request.Id,
            request.ClinicId,
            request.FirstName,
            request.LastName,
            request.Specialty);

    public static Provider ToEntity(this ProviderRegistrationRequest request) =>
        Provider.Register(
            request.Id,
            request.FirstName,
            request.LastName,
            request.Specialty);

    public static ProviderResponse ToResponse(this Provider provider) =>
        new(
            provider.Id,
            provider.ClinicId,
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            provider.VerificationStatus.ToString());

    public static void ApplyTo(this ProviderRequest request, Provider provider)
    {
        provider.SetFirstName(request.FirstName);
        provider.SetLastName(request.LastName);
        provider.SetSpecialty(request.Specialty);
    }
}
