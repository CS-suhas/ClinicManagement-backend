using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Mapping;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Providers.Services;

public class ProviderService(IProviderRepository providerRepository, IClinicRepository clinicRepository) : IProviderService
{
    public async Task<ProviderResponse> GetProviderByIdAsync(Guid providerId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        var provider = await providerRepository.GetProviderByIdAsync(providerId)
            ?? throw new ResourceNotFoundException("Provider", providerId);
        return provider.ToResponse();
    }

    public async Task<List<ProviderResponse>> GetAllProvidersAsync()
    {
        var providers = await providerRepository.GetAllProvidersAsync();
        return providers.Select(item => item.ToResponse()).ToList();
    }

    public async Task<Guid> AddProviderAsync(ProviderRequest request)
    {
        ValidateRequest(request);

        var clinicExists = await clinicRepository.ClinicExistsAsync(request.ClinicId);
        if (!clinicExists)
        {
            throw new ResourceNotFoundException("Clinic", request.ClinicId);
        }
        var provider = request.ToEntity();
        await providerRepository.AddProviderAsync(provider, request.ClinicId);
        return provider.Id;
    }

    public async Task<Guid> UpdateProviderAsync(ProviderRequest request)
    {
        ValidateRequest(request);

        var provider = await providerRepository.GetProviderByIdAsync(request.Id);
        if (provider == null)
        {
            throw new ResourceNotFoundException("Provider", request.Id);
        }
        var clinic = await clinicRepository.ClinicExistsAsync(request.ClinicId);
        if (!clinic)
        {
            throw new ResourceNotFoundException("Clinic", request.ClinicId);
        }
        request.ApplyTo(provider);
        await providerRepository.UpdateProviderAsync(provider, request.ClinicId);
        return provider.Id;
    }

    public async Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        var provider = await providerRepository.GetProviderByIdAsync(providerId);
        if (provider == null)
        {
            throw new ResourceNotFoundException("Provider", providerId);
        }

        if (await providerRepository.HasAppointmentsAsync(providerId))
        {
            throw new ProviderHasAppointmentsException(providerId);
        }

        await providerRepository.DeleteProviderAsync(provider.Id);
        return providerId;
    }

    private static void ValidateRequest(ProviderRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Provider ID");
        RequestValidator.EnsureGuid(request.ClinicId, "clinicId", "Clinic ID");
        RequestValidator.EnsureText(request.FirstName, "firstName", "First name");
        RequestValidator.EnsureText(request.LastName, "lastName", "Last name");
        RequestValidator.EnsureText(request.Specialty, "specialty", "Specialty");
    }
}
