using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Providers.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetProviderByIdAsync(Guid providerId);
    Task<IEnumerable<Provider>> GetAllProvidersAsync();
    Task<Guid> AddProviderAsync(Provider request, Guid clinicId);
    Task<Guid> UpdateProviderAsync(Provider request, Guid clinicId);
    Task<Guid> DeleteProviderAsync(Guid providerId);
    Task<bool> HasAppointmentsAsync(Guid providerId);
}
