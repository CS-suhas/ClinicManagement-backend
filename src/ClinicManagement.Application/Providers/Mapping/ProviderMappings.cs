using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Providers.Mapping;

public static class ProviderMappings
{
    public static Provider ToEntity(this ProviderRequest request, Clinic clinic) =>
        Provider.Create(request.Id, request.FirstName, request.LastName, request.Specialty, clinic);

    public static ProviderResponse ToResponse(this Provider provider) =>
        new(
            provider.Id,
            provider.Clinic.Id,
            $"{provider.FirstName} {provider.LastName}",
            provider.Specialty);

    public static void ApplyTo(this ProviderRequest request, Provider provider, Clinic clinic)
    {
        provider.SetFirstName(request.FirstName);
        provider.SetLastName(request.LastName);
        provider.SetSpecialty(request.Specialty);
        provider.SetClinic(clinic);
    }
}
