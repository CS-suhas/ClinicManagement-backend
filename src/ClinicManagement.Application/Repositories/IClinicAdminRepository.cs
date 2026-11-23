using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Repositories;

public interface IClinicAdminRepository
{
    Task<bool> IsAdminOfClinicAsync(string identitySubject, Guid clinicId);
    Task<bool> IsAnyAdminAsync(string identitySubject);
    Task<IReadOnlyList<Guid>> GetClinicIdsAsync(string identitySubject);
    Task AddAsync(ClinicAdmin clinicAdmin);
}
