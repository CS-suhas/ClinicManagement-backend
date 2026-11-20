using ClinicManagement.Application.Clinics.Models;

namespace ClinicManagement.Application.Clinics.Interfaces;

public interface IClinicService
{
    Task<ClinicResponse> GetClinicByIdAsync(Guid clinicId);
    Task<IEnumerable<ClinicResponse>> GetAllClinicsAsync();
    Task<Guid> AddClinicAsync(ClinicRequest request);
    Task<Guid> UpdateClinicAsync(ClinicRequest request);
    Task<Guid> DeleteClinicAsync(Guid clinicId);
}
