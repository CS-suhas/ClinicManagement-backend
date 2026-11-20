using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Patients.Mapping;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Patients.Services;

public class PatientService(IPatientRepository patientRepository) : IPatientService
{
    public async Task<PatientResponse> GetPatientByIdAsync(Guid patientId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");

        var patient = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);
        return patient.ToResponse();
    }

    public async Task<List<PatientResponse>> GetAllPatientsAsync()
    {
        var patients = await patientRepository.GetAllPatientsAsync();
        return patients.Select(p => p.ToResponse()).ToList();
    }

    public async Task<Guid> AddPatientAsync(PatientRequest patient)
    {
        ValidateRequest(patient);

        var newPatient = patient.ToEntity();

        await patientRepository.AddPatientAsync(newPatient);
        return newPatient.Id;
    }

    public async Task<Guid> UpdatePatientAsync(PatientRequest request)
    {
        ValidateRequest(request);

        var patient = await patientRepository.GetPatientByIdAsync(request.Id)
            ?? throw new ResourceNotFoundException("Patient", request.Id);

        request.ApplyTo(patient);
        await patientRepository.UpdatePatientAsync(patient);
        return patient.Id;
    }

    public async Task<Guid> DeletePatientAsync(Guid patientId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");

        var patient = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);

        if (await patientRepository.HasDependentRecordsAsync(patientId))
        {
            throw new PatientHasRecordsException(patientId);
        }

        await patientRepository.DeletePatientAsync(patient.Id);
        return patientId;
    }

    private static void ValidateRequest(PatientRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Patient ID");
        RequestValidator.EnsureText(request.FirstName, "firstName", "First name");
        RequestValidator.EnsureText(request.LastName, "lastName", "Last name");
        RequestValidator.EnsureDateOfBirth(request.DateOfBirth);
        RequestValidator.EnsureText(request.PhoneNumber, "phoneNumber", "Phone number");
        RequestValidator.EnsureEmail(request.Email);
    }
}