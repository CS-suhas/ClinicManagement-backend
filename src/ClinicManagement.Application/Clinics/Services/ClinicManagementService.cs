using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Mapping;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.Clinics.Services;

public class ClinicManagementService(IApplicationDbContext dbContext) : IClinicManagementService
{
    public async Task<ClinicResponse?> GetClinicByIdAsync(Guid clinicId)
    {
        var clinic = await dbContext.Clinics.FindAsync(clinicId);
        return clinic?.ToResponse();
    }

    public async Task<List<ClinicResponse>> GetAllClinicsAsync()
    {
        var clinics = await dbContext.Clinics
            .AsNoTracking()
            .ToListAsync();

        return clinics.Select(clinic => clinic.ToResponse()).ToList();
    }

    public async Task<Guid> AddClinicAsync(ClinicRequest request)
    {
        var clinic = request.ToEntity();
        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();
        return clinic.Id;
    }

    public async Task<Guid> UpdateClinicAsync(ClinicRequest request)
    {
        var clinic = await dbContext.Clinics.FindAsync(request.Id)
            ?? throw new KeyNotFoundException($"Clinic '{request.Id}' was not found.");
        request.ApplyTo(clinic);
        await dbContext.SaveChangesAsync();
        return clinic.Id;
    }

    public async Task<Guid> DeleteClinicAsync(Guid clinicId)
    {
        var clinic = await dbContext.Clinics.FindAsync(clinicId)
            ?? throw new KeyNotFoundException($"Clinic '{clinicId}' was not found.");
        dbContext.Clinics.Remove(clinic);
        await dbContext.SaveChangesAsync();
        return clinicId;
    }
}
