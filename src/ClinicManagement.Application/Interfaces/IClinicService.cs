using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface IClinicService
{
    Task<ClinicResponse> GetClinicByIdAsync(Guid clinicId);
    Task<IEnumerable<ClinicListItemResponse>> GetAllClinicsAsync();
    Task<ClinicSummaryResponse> GetClinicSummaryAsync(Guid clinicId);
    Task<List<ProviderResponse>> GetProvidersByClinicIdAsync(Guid clinicId);
    Task<List<AppointmentResponse>> GetAppointmentsByClinicIdAsync(Guid clinicId);
    Task<Guid> AddClinicAsync(ClinicRequest request);
    Task<Guid> UpdateClinicAsync(ClinicRequest request);
    Task<Guid> DeleteClinicAsync(Guid clinicId);
    Task<PagedResponse<ClinicListItemResponse>> SearchClinicsAsync(PageQuery query);
    Task<Guid> AddClinicAdminAsync(Guid clinicId, string identitySubject);
}
