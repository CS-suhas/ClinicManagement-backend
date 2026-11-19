using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Patients.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetPatientByIdAsync(Guid patientId);
    Task<IEnumerable<Patient>> GetAllPatientsAsync();
    Task<Guid> AddPatientAsync(Patient patient);
    Task<Guid> UpdatePatientAsync(Patient patient);
    Task<Guid> DeletePatientAsync(Guid patientId);
}