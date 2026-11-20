using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Appointments.Models;

namespace ClinicManagement.Application.Patients.Interfaces;

public interface IPatientService
{
    Task<PatientResponse> GetPatientByIdAsync(Guid patientId);
    Task<List<PatientResponse>> GetAllPatientsAsync();
    Task<IEnumerable<PatientAllergyResponse>> GetPatientAllergiesAsync(Guid patientId);
    Task<List<AppointmentResponse>> GetAppointmentsByPatientIdAsync(Guid patientId);
    Task<Guid> AddPatientAsync(PatientRequest patient);
    Task<Guid> UpdatePatientAsync(PatientRequest patient);
    Task<Guid> DeletePatientAsync(Guid patientId);
}