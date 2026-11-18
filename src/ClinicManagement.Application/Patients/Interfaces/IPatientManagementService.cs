using ClinicManagement.Application.Patients.Models;

namespace ClinicManagement.Application.Patients.Interfaces;

public interface IPatientManagementService
{
    Task<PatientResponse?> GetPatientByIdAsync(Guid patientId);
    Task<List<PatientResponse>> GetAllPatientsAsync();
    Task<Guid> AddPatientAsync(PatientRequest request);
    Task<Guid> UpdatePatientAsync(PatientRequest request);
    Task<Guid> DeletePatientAsync(Guid patientId);
}