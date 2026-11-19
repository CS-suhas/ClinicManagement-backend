using ClinicManagement.Application.AllergyTypes.Models;

namespace ClinicManagement.Application.AllergyTypes.Interfaces;

public interface IAllergyTypeService
{
    Task<AllergyTypeResponse?> GetAllergyTypeByIdAsync(Guid allergyTypeId);
    Task<IEnumerable<AllergyTypeResponse>> GetAllAllergyTypesAsync();
    Task<Guid> AddAllergyTypeAsync(AllergyTypeRequest request);
    Task<Guid> UpdateAllergyTypeAsync(AllergyTypeRequest request);
    Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId);
}
