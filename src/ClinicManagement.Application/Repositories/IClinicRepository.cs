using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Repositories;

public interface IClinicRepository
{
    Task<Clinic?> GetClinicByIdAsync(Guid clinicId);
    Task<IEnumerable<Clinic>> GetAllClinicsAsync();
    Task<Guid> AddClinicAsync(Clinic request);
    Task<Guid> UpdateClinicAsync(Clinic request);
    Task<Guid> DeleteClinicAsync(Guid clinicId);
    Task<bool> ClinicExistsAsync(Guid clinicId);
    Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(PageQuery query, int offset);
}
