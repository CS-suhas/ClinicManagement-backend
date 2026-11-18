using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Allergies.Mapping;
using ClinicManagement.Application.Allergies.Models;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Allergies.Services;

public class AllergyManagementService(IApplicationDbContext dbContext) : IAllergyManagementService
{
    public async Task<AllergyResponse?> GetAllergyByIdAsync(Guid allergyId)
    {
        var allergy = await dbContext.Allergies
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.AllergyType)
            .FirstOrDefaultAsync(item => item.Id == allergyId);

        return allergy?.ToResponse();
    }

    public async Task<List<AllergyResponse>> GetAllAllergiesAsync()
    {
        var allergies = await dbContext.Allergies
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.AllergyType)
            .ToListAsync();

        return allergies.Select(item => item.ToResponse()).ToList();
    }

    public async Task<Guid> AddAllergyAsync(AllergyRequest request)
    {
        var patient = await dbContext.Patients.FindAsync(request.PatientId)
            ?? throw new KeyNotFoundException($"Patient '{request.PatientId}' was not found.");
        var allergyType = await dbContext.AllergyTypes.FindAsync(request.AllergyTypeId)
            ?? throw new KeyNotFoundException($"Allergy type '{request.AllergyTypeId}' was not found.");

        await EnsureNotDuplicateAsync(request.PatientId, request.AllergyTypeId);

        var allergy = request.ToEntity(patient, allergyType);
        dbContext.Allergies.Add(allergy);
        await dbContext.SaveChangesAsync();

        return allergy.Id;
    }

    public async Task<Guid> UpdateAllergyAsync(AllergyRequest request)
    {
        var allergy = await dbContext.Allergies.FindAsync(request.Id)
            ?? throw new KeyNotFoundException($"Allergy '{request.Id}' was not found.");
        var patient = await dbContext.Patients.FindAsync(request.PatientId)
            ?? throw new KeyNotFoundException($"Patient '{request.PatientId}' was not found.");
        var allergyType = await dbContext.AllergyTypes.FindAsync(request.AllergyTypeId)
            ?? throw new KeyNotFoundException($"Allergy type '{request.AllergyTypeId}' was not found.");

        await EnsureNotDuplicateAsync(request.PatientId, request.AllergyTypeId, request.Id);

        request.ApplyTo(allergy, patient, allergyType);
        await dbContext.SaveChangesAsync();

        return allergy.Id;
    }

    public async Task<Guid> DeleteAllergyAsync(Guid allergyId)
    {
        var allergy = await dbContext.Allergies.FindAsync(allergyId)
            ?? throw new KeyNotFoundException($"Allergy '{allergyId}' was not found.");

        dbContext.Allergies.Remove(allergy);
        await dbContext.SaveChangesAsync();

        return allergyId;
    }

    private async Task EnsureNotDuplicateAsync(
        Guid patientId,
        Guid allergyTypeId,
        Guid? allergyId = null)
    {
        var query = dbContext.Allergies
            .AsNoTracking()
            .Where(allergy =>
                allergy.Patient.Id == patientId &&
                allergy.AllergyType.Id == allergyTypeId);

        if (allergyId.HasValue)
        {
            query = query.Where(allergy => allergy.Id != allergyId.Value);
        }

        var duplicateExists = await query.AnyAsync();

        if (duplicateExists)
        {
            throw new DuplicatePatientAllergyException();
        }
    }
}
