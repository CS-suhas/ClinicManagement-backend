using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Allergies.Interfaces;

public interface IAllergyTypeRepository
{
    Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId);
    Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync();
    Task<Guid> AddAllergyTypeAsync(AllergyType request);
    Task<Guid> UpdateAllergyTypeAsync(AllergyType request);
    Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId);
}
