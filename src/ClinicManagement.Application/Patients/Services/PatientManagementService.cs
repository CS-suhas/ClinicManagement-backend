using Microsoft.EntityFrameworkCore;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Mapping;
using ClinicManagement.Application.Patients.Models;

namespace ClinicManagement.Application.Patients.Services;

public class PatientManagementService(IApplicationDbContext dbContext) : IPatientManagementService
{
    public async Task<PatientResponse?> GetPatientByIdAsync(Guid patientId)
    {
        var patient = await dbContext.Patients.FindAsync(patientId);
        if (patient == null)
        {
            return null;
        }
        return patient.ToResponse();
    }

    public async Task<List<PatientResponse>> GetAllPatientsAsync()
    {
        return await dbContext.Patients
            .Select(patient => new PatientResponse(
                Id: patient.Id,
                Name: $"{patient.FirstName} {patient.LastName}",
                DateOfBirth: patient.DateOfBirth,
                PhoneNumber: patient.PhoneNumber,
                EmailAddress: patient.Email))
            .ToListAsync();
    }

    public async Task<Guid> AddPatientAsync(PatientRequest patient)
    {
        var newPatient = patient.ToEntity();

        dbContext.Patients.Add(newPatient);
        await dbContext.SaveChangesAsync();
        return newPatient.Id;
    }

    public async Task<Guid> UpdatePatientAsync(PatientRequest request)
    {
        var patient = await dbContext.Patients.FindAsync(request.Id)
            ?? throw new KeyNotFoundException($"Patient '{request.Id}' was not found.");

        request.ApplyTo(patient);
        await dbContext.SaveChangesAsync();
        return patient.Id;
    }

    public async Task<Guid> DeletePatientAsync(Guid patientId)
    {
        var patient = await dbContext.Patients.FindAsync(patientId)
            ?? throw new KeyNotFoundException($"Patient '{patientId}' was not found.");

        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync();
        return patientId;
    }
}