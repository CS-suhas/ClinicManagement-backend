using ClinicManagement.Application.Providers.Models;

namespace ClinicManagement.Application.Providers.Interfaces;

public interface IProviderManagementService
{
    Task<ProviderResponse?> GetProviderByIdAsync(Guid providerId);
    Task<List<ProviderResponse>> GetAllProvidersAsync();
    Task<Guid> AddProviderAsync(ProviderRequest request);
    Task<Guid> UpdateProviderAsync(ProviderRequest request);
    Task<Guid> DeleteProviderAsync(Guid providerId);
}
