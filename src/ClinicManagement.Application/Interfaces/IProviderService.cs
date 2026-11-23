using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Domain.Enums;

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
    Task<PagedResponse<ProviderResponse>> SearchProvidersAsync(
        PageQuery query,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus = null);
    /// <summary>Verified providers with a clinic: what patients are allowed to browse.</summary>
    Task<PagedResponse<ProviderResponse>> SearchProviderDirectoryAsync(PageQuery query, Guid? clinicId);
    /// <summary>Self-registration. The provider stays hidden until an admin verifies them.</summary>
    Task<Guid> RegisterProviderAsync(ProviderRegistrationRequest request);
    Task VerifyProviderAsync(Guid providerId);
    Task RejectProviderAsync(Guid providerId);
    Task<List<AppointmentListResponse>> GetProviderCalendarAsync(
        Guid providerId,
        DateTimeOffset from,
        DateTimeOffset to,
        bool includeInactive);
}
