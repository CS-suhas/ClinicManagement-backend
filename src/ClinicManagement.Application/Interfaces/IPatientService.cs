using ClinicManagement.Application.Patients.Models;

namespace ClinicManagement.Application.Patients.Interfaces;

public interface IPatientService
{
    Task<PatientResponse?> GetPatientByIdAsync(Guid patientId);
    Task<List<PatientResponse>> GetAllPatientsAsync();
    Task<Guid> AddPatientAsync(PatientRequest patient);
    Task<Guid> UpdatePatientAsync(PatientRequest patient);
    Task<Guid> DeletePatientAsync(Guid patientId);
}