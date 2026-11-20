using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Providers.Models;

namespace ClinicManagement.Application.Clinics.Interfaces;

public interface IClinicService
{
    Task<ClinicResponse> GetClinicByIdAsync(Guid clinicId);
    Task<IEnumerable<ClinicResponse>> GetAllClinicsAsync();
    Task<List<ProviderResponse>> GetProvidersByClinicIdAsync(Guid clinicId);
    Task<List<AppointmentResponse>> GetAppointmentsByClinicIdAsync(Guid clinicId);
    Task<Guid> AddClinicAsync(ClinicRequest request);
    Task<Guid> UpdateClinicAsync(ClinicRequest request);
    Task<Guid> DeleteClinicAsync(Guid clinicId);
}
