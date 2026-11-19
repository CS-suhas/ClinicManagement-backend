using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Mapping;
using ClinicManagement.Application.Patients.Models;

namespace ClinicManagement.Application.Patients.Services;

public class PatientService(IPatientRepository patientRepository) : IPatientService
{
    public async Task<PatientResponse?> GetPatientByIdAsync(Guid patientId)
    {
        var patient = await patientRepository.GetPatientByIdAsync(patientId);
        if (patient == null)
        {
            return null;
        }
        return patient.ToResponse();
    }

    public async Task<List<PatientResponse>> GetAllPatientsAsync()
    {
        var patients = await patientRepository.GetAllPatientsAsync();
        return patients.Select(p => p.ToResponse()).ToList();
    }

    public async Task<Guid> AddPatientAsync(PatientRequest patient)
    {
        var newPatient = patient.ToEntity();

        await patientRepository.AddPatientAsync(newPatient);
        return newPatient.Id;
    }

    public async Task<Guid> UpdatePatientAsync(PatientRequest request)
    {
        var patient = await patientRepository.GetPatientByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Patient '{request.Id}' was not found.");

        request.ApplyTo(patient);
        await patientRepository.UpdatePatientAsync(patient);
        return patient.Id;
    }

    public async Task<Guid> DeletePatientAsync(Guid patientId)
    {
        var patient = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new KeyNotFoundException($"Patient '{patientId}' was not found.");

        await patientRepository.DeletePatientAsync(patient.Id);
        return patientId;
    }
}