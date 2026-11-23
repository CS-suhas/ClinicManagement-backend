using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Patients.Interfaces;

public interface IPatientService
{
    Task<PatientResponse> GetPatientByIdAsync(Guid patientId);
    Task<List<PatientListItemResponse>> GetAllPatientsAsync();
    Task<IEnumerable<PatientAllergyResponse>> GetPatientAllergiesAsync(Guid patientId);
    Task<List<AppointmentResponse>> GetAppointmentsByPatientIdAsync(Guid patientId);
    Task<Guid> AddPatientAsync(PatientRequest patient);
    Task<Guid> UpdatePatientAsync(PatientRequest patient);
    Task<Guid> DeletePatientAsync(Guid patientId);
    Task<PagedResponse<PatientListItemResponse>> SearchPatientsAsync(PageQuery query);
    Task<PagedResponse<PatientAllergyResponse>> SearchPatientAllergiesAsync(Guid patientId, PageQuery query);
    Task<PagedResponse<AllergyTypeResponse>> GetSelectableAllergyTypesAsync(
        Guid patientId,
        Guid? selectedAllergyId,
        PageQuery query);
    Task<Guid> UpsertPatientAllergyAsync(Guid patientId, PatientAllergyRequest request);
    Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId);
}