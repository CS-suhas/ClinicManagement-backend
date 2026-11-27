using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Interfaces;

public interface IProviderService
{
    Task<ProviderResponse> GetProviderByIdAsync(Guid providerId);
    Task<List<ProviderResponse>> GetAllProvidersAsync();
    Task<ClinicResponse> GetClinicByProviderIdAsync(Guid providerId);
    Task<List<AppointmentResponse>> GetAppointmentsByProviderIdAsync(Guid providerId);
    Task<Guid> AddProviderAsync(ProviderRequest request);
    Task<Guid> UpdateProviderAsync(ProviderRequest request);
    Task<Guid> DeleteProviderAsync(Guid providerId);
    Task<PagedResponse<ProviderResponse>> SearchProvidersAsync(
        PageQuery query,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus = null);
    Task<PagedResponse<ProviderResponse>> SearchProviderDirectoryAsync(PageQuery query, Guid? clinicId);
    Task<Guid> RegisterProviderAsync(ProviderRegistrationRequest request);
    Task VerifyProviderAsync(Guid providerId);
    Task RejectProviderAsync(Guid providerId);
    Task<List<AppointmentListResponse>> GetProviderCalendarAsync(
        Guid providerId,
        DateTimeOffset from,
        DateTimeOffset to,
        bool includeInactive);
    Task<PagedResponse<ProviderPatientRosterItem>> GetPatientRosterAsync(Guid providerId, PageQuery query);
}
