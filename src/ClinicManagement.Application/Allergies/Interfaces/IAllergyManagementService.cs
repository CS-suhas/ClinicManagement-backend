using ClinicManagement.Application.Allergies.Models;

namespace ClinicManagement.Application.Allergies.Interfaces;

public interface IAllergyManagementService
{
    Task<AllergyResponse?> GetAllergyByIdAsync(Guid allergyId);
    Task<List<AllergyResponse>> GetAllAllergiesAsync();
    Task<Guid> AddAllergyAsync(AllergyRequest request);
    Task<Guid> UpdateAllergyAsync(AllergyRequest request);
    Task<Guid> DeleteAllergyAsync(Guid allergyId);
}
