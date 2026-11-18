using ClinicManagement.Application.AllergyTypes.Interfaces;
using ClinicManagement.Application.AllergyTypes.Mapping;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Application.AllergyTypes.Services;

public class AllergyTypeManagementService(IApplicationDbContext dbContext) : IAllergyTypeManagementService
{
    public async Task<AllergyTypeResponse?> GetAllergyTypeByIdAsync(Guid allergyTypeId)
    {
        var allergyType = await dbContext.AllergyTypes.FindAsync(allergyTypeId);
        return allergyType?.ToResponse();
    }

    public async Task<List<AllergyTypeResponse>> GetAllAllergyTypesAsync()
    {
        var allergyTypes = await dbContext.AllergyTypes
            .AsNoTracking()
            .ToListAsync();

        return allergyTypes.Select(item => item.ToResponse()).ToList();
    }

    public async Task<Guid> AddAllergyTypeAsync(AllergyTypeRequest request)
    {
        var allergyType = request.ToEntity();
        dbContext.AllergyTypes.Add(allergyType);
        await dbContext.SaveChangesAsync();
        return allergyType.Id;
    }

    public async Task<Guid> UpdateAllergyTypeAsync(AllergyTypeRequest request)
    {
        var allergyType = await dbContext.AllergyTypes.FindAsync(request.Id)
            ?? throw new KeyNotFoundException($"Allergy type '{request.Id}' was not found.");
        request.ApplyTo(allergyType);
        await dbContext.SaveChangesAsync();
        return allergyType.Id;
    }

    public async Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId)
    {
        var allergyType = await dbContext.AllergyTypes.FindAsync(allergyTypeId)
            ?? throw new KeyNotFoundException($"Allergy type '{allergyTypeId}' was not found.");
        dbContext.AllergyTypes.Remove(allergyType);
        await dbContext.SaveChangesAsync();
        return allergyTypeId;
    }
}
