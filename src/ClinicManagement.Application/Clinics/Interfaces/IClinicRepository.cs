using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Clinics.Interfaces;

public interface IClinicRepository
{
    Task<Clinic?> GetClinicByIdAsync(Guid clinicId);
    Task<IEnumerable<Clinic>> GetAllClinicsAsync();
    Task<Guid> AddClinicAsync(Clinic request);
    Task<Guid> UpdateClinicAsync(Clinic request);
    Task<Guid> DeleteClinicAsync(Guid clinicId);
}
