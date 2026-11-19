using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Mapping;
using ClinicManagement.Application.Providers.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Providers.Services;

public class ProviderService(IProviderRepository providerRepository, IClinicRepository clinicRepository) : IProviderService
{
    public async Task<ProviderResponse?> GetProviderByIdAsync(Guid providerId)
    {
        var provider = await providerRepository.GetProviderByIdAsync(providerId);
        return provider?.ToResponse();
    }

    public async Task<List<ProviderResponse>> GetAllProvidersAsync()
    {
        var providers = await providerRepository.GetAllProvidersAsync();
        return providers.Select(item => item.ToResponse()).ToList();
    }

    public async Task<Guid> AddProviderAsync(ProviderRequest request)
    {
        var clinicExists = await clinicRepository.ClinicExistsAsync(request.ClinicId);
        if (!clinicExists)
        {
            throw new KeyNotFoundException($"Clinic '{request.ClinicId}' was not found.");
        }
        var provider = request.ToEntity();
        await providerRepository.AddProviderAsync(provider, request.ClinicId);
        return provider.Id;
    }

    public async Task<Guid> UpdateProviderAsync(ProviderRequest request)
    {
        var provider = await providerRepository.GetProviderByIdAsync(request.Id);
        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider '{request.Id}' was not found.");
        }
        var clinic = await clinicRepository.ClinicExistsAsync(request.ClinicId);
        if (!clinic)
        {
            throw new KeyNotFoundException($"Clinic '{request.ClinicId}' was not found.");
        }
        request.ApplyTo(provider);
        await providerRepository.UpdateProviderAsync(provider, request.ClinicId);
        return provider.Id;
    }

    public async Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        var provider = await providerRepository.GetProviderByIdAsync(providerId);
        if (provider == null)
        {
            throw new KeyNotFoundException($"Provider '{providerId}' was not found.");
        }
        await providerRepository.DeleteProviderAsync(provider.Id);
        return providerId;
    }
}
