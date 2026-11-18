using ClinicManagement.Application.AllergyTypes.Models;

namespace ClinicManagement.Application.AllergyTypes.Interfaces;

public interface IAllergyTypeManagementService
{
    Task<AllergyTypeResponse?> GetAllergyTypeByIdAsync(Guid allergyTypeId);
    Task<List<AllergyTypeResponse>> GetAllAllergyTypesAsync();
    Task<Guid> AddAllergyTypeAsync(AllergyTypeRequest request);
    Task<Guid> UpdateAllergyTypeAsync(AllergyTypeRequest request);
    Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId);
}
