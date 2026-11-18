using ClinicManagement.Application.Clinics.Models;

namespace ClinicManagement.Application.Clinics.Interfaces;

public interface IClinicManagementService
{
    Task<ClinicResponse?> GetClinicByIdAsync(Guid clinicId);
    Task<List<ClinicResponse>> GetAllClinicsAsync();
    Task<Guid> AddClinicAsync(ClinicRequest request);
    Task<Guid> UpdateClinicAsync(ClinicRequest request);
    Task<Guid> DeleteClinicAsync(Guid clinicId);
}
