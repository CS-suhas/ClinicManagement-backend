using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Mapping;
using ClinicManagement.Application.Providers.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Providers.Services;

public class ProviderManagementService(IApplicationDbContext dbContext) : IProviderManagementService
{
    public async Task<ProviderResponse?> GetProviderByIdAsync(Guid providerId)
    {
        var provider = await dbContext.Providers
            .AsNoTracking()
            .Include(item => item.Clinic)
            .FirstOrDefaultAsync(item => item.Id == providerId);

        return provider?.ToResponse();
    }

    public async Task<List<ProviderResponse>> GetAllProvidersAsync()
    {
        var providers = await dbContext.Providers
            .AsNoTracking()
            .Include(item => item.Clinic)
            .ToListAsync();

        return providers.Select(item => item.ToResponse()).ToList();
    }

    public async Task<Guid> AddProviderAsync(ProviderRequest request)
    {
        var clinic = await dbContext.Clinics.FindAsync(request.ClinicId)
            ?? throw new KeyNotFoundException($"Clinic '{request.ClinicId}' was not found.");
        var provider = request.ToEntity(clinic);
        dbContext.Providers.Add(provider);
        await dbContext.SaveChangesAsync();
        return provider.Id;
    }

    public async Task<Guid> UpdateProviderAsync(ProviderRequest request)
    {
        var provider = await dbContext.Providers.FindAsync(request.Id)
            ?? throw new KeyNotFoundException($"Provider '{request.Id}' was not found.");
        var clinic = await dbContext.Clinics.FindAsync(request.ClinicId)
            ?? throw new KeyNotFoundException($"Clinic '{request.ClinicId}' was not found.");

        request.ApplyTo(provider, clinic);
        await dbContext.SaveChangesAsync();
        return provider.Id;
    }

    public async Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        var provider = await dbContext.Providers.FindAsync(providerId)
            ?? throw new KeyNotFoundException($"Provider '{providerId}' was not found.");
        dbContext.Providers.Remove(provider);
        await dbContext.SaveChangesAsync();
        return providerId;
    }
}
