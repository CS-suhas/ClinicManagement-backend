using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Clinics.Interfaces;

public interface IClinicService
{
    Task<ClinicResponse> GetClinicByIdAsync(Guid clinicId);
    Task<IEnumerable<ClinicListItemResponse>> GetAllClinicsAsync();
    /// <summary>Clinic with provider count, today's appointment count, pending join requests (admins of the clinic only), top specialties and up to five providers.</summary>
    Task<ClinicSummaryResponse> GetClinicSummaryAsync(Guid clinicId);
    Task<List<ProviderResponse>> GetProvidersByClinicIdAsync(Guid clinicId);
    Task<List<AppointmentResponse>> GetAppointmentsByClinicIdAsync(Guid clinicId);
    Task<Guid> AddClinicAsync(ClinicRequest request);
    Task<Guid> UpdateClinicAsync(ClinicRequest request);
    Task<Guid> DeleteClinicAsync(Guid clinicId);
    Task<PagedResponse<ClinicListItemResponse>> SearchClinicsAsync(PageQuery query);
    Task<Guid> AddClinicAdminAsync(Guid clinicId, string identitySubject);
}
