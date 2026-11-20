using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Models;

namespace ClinicManagement.Application.AllergyTypes.Interfaces;

public interface IAllergyTypeService
{
    Task<AllergyTypeResponse> GetAllergyTypeByIdAsync(Guid allergyTypeId);
    Task<IEnumerable<AllergyTypeResponse>> GetAllAllergyTypesAsync();
    Task<IEnumerable<PatientResponse>> GetPatientsByAllergyTypeIdAsync(Guid allergyTypeId);
    Task<Guid> AddAllergyTypeAsync(AllergyTypeRequest request);
    Task<Guid> UpdateAllergyTypeAsync(AllergyTypeRequest request);
    Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId);
    Task<PagedResponse<AllergyTypeResponse>> SearchAllergyTypesAsync(PageQuery query);
}
