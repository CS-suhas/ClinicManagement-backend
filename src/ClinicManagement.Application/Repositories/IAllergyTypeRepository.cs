using ClinicManagement.Domain.Entities;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface IAllergyTypeRepository
{
    Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId);
    Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync();
    Task<IEnumerable<Patient>> GetPatientsByAllergyTypeIdAsync(Guid allergyTypeId);
    Task<bool> HasPatientsWithAllergyTypeAsync(Guid allergyTypeId);
    Task<Guid> AddAllergyTypeAsync(AllergyType request);
    Task<Guid> UpdateAllergyTypeAsync(AllergyType request);
    Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId);
    Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> SearchAllergyTypesAsync(PageQuery query, int offset);
    Task<IReadOnlyDictionary<Guid, int>> GetPatientCountsByAllergyTypeAsync();
}
