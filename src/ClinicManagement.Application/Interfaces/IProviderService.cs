using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Clinics.Models;

namespace ClinicManagement.Application.Providers.Interfaces;

public interface IProviderService
{
    Task<ProviderResponse> GetProviderByIdAsync(Guid providerId);
    Task<List<ProviderResponse>> GetAllProvidersAsync();
    Task<ClinicResponse> GetClinicByProviderIdAsync(Guid providerId);
    Task<List<AppointmentResponse>> GetAppointmentsByProviderIdAsync(Guid providerId);
    Task<Guid> AddProviderAsync(ProviderRequest request);
    Task<Guid> UpdateProviderAsync(ProviderRequest request);
    Task<Guid> DeleteProviderAsync(Guid providerId);
    Task<PagedResponse<ProviderResponse>> SearchProvidersAsync(PageQuery query, Guid? clinicId);
}
