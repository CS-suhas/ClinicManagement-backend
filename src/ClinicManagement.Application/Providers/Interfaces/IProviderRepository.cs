using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Providers.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetProviderByIdAsync(Guid providerId);
    Task<IEnumerable<Provider>> GetAllProvidersAsync();
    Task<Guid> AddProviderAsync(Provider request);
    Task<Guid> UpdateProviderAsync(Provider request);
    Task<Guid> DeleteProviderAsync(Guid providerId);
}
