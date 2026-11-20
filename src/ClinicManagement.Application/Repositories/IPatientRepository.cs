using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Patients.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetPatientByIdAsync(Guid patientId);
    Task<IEnumerable<Patient>> GetAllPatientsAsync();
    Task<Guid> AddPatientAsync(Patient patient);
    Task<Guid> UpdatePatientAsync(Patient patient);
    Task<Guid> DeletePatientAsync(Guid patientId);
    Task<bool> PatientExistsAsync(Guid patientId);
    Task<bool> HasDependentRecordsAsync(Guid patientId);
    Task<IEnumerable<PatientAllergy>> GetAllergiesByPatientIdAsync(Guid patientId);
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(PageQuery query, int offset);
    Task<(IReadOnlyList<PatientAllergy> Items, int TotalCount)> SearchPatientAllergiesAsync(
        Guid patientId,
        PageQuery query,
        int offset);
    Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> GetSelectableAllergyTypesAsync(
        Guid patientId,
        Guid? selectedAllergyId,
        PageQuery query,
        int offset);
    Task<Guid> UpsertPatientAllergyAsync(
        Guid patientId,
        Guid allergyId,
        AllergySeverity severity,
        string? notes);
    Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId);
}