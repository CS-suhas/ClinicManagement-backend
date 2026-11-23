using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Repositories;

public interface IClinicAdminRepository
{
    Task<bool> IsAdminOfClinicAsync(Guid userId, Guid clinicId);
    Task<IReadOnlyList<Guid>> GetClinicIdsAsync(Guid userId);
    Task AddAsync(ClinicAdmin clinicAdmin);
}
