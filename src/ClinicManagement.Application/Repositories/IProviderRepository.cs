using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Providers.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetProviderByIdAsync(Guid providerId);
    Task<IEnumerable<Provider>> GetAllProvidersAsync();
    Task<Guid> AddProviderAsync(Provider request, Guid clinicId);
    Task<Guid> UpdateProviderAsync(Provider request, Guid clinicId);
    Task<Guid> DeleteProviderAsync(Guid providerId);
    Task<bool> HasAppointmentsAsync(Guid providerId);
    Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId);
    Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId);
}
