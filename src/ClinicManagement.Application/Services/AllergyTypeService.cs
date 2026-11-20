using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.AllergyTypes.Interfaces;
using ClinicManagement.Application.AllergyTypes.Mapping;
using ClinicManagement.Application.AllergyTypes.Models;

namespace ClinicManagement.Application.AllergyTypes.Services;

public class AllergyTypeService(IAllergyTypeRepository allergyTypeRepository) : IAllergyTypeService
{
    public async Task<AllergyTypeResponse> GetAllergyTypeByIdAsync(Guid allergyTypeId)
    {
        var allergyType = await allergyTypeRepository.GetAllergyTypeByIdAsync(allergyTypeId)
            ?? throw new ResourceNotFoundException("Allergy type", allergyTypeId);
        return allergyType.ToResponse();
    }

    public async Task<IEnumerable<AllergyTypeResponse>> GetAllAllergyTypesAsync()
    {
        var allergyTypes = await allergyTypeRepository.GetAllAllergyTypesAsync();

        return allergyTypes.Select(item => item.ToResponse());
    }

    public async Task<Guid> AddAllergyTypeAsync(AllergyTypeRequest request)
    {
        var allergyType = request.ToEntity();
        await allergyTypeRepository.AddAllergyTypeAsync(allergyType);
        return allergyType.Id;
    }

    public async Task<Guid> UpdateAllergyTypeAsync(AllergyTypeRequest request)
    {
        var allergyType = await allergyTypeRepository.GetAllergyTypeByIdAsync(request.Id);
        if (allergyType == null)
        {
            throw new ResourceNotFoundException("Allergy type", request.Id);
        }
        request.ApplyTo(allergyType);
        await allergyTypeRepository.UpdateAllergyTypeAsync(allergyType);
        return allergyType.Id;
    }

    public async Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId)
    {
        var allergyType = await allergyTypeRepository.GetAllergyTypeByIdAsync(allergyTypeId);
        if (allergyType == null)
        {
            throw new ResourceNotFoundException("Allergy type", allergyTypeId);
        }
        await allergyTypeRepository.DeleteAllergyTypeAsync(allergyType.Id);
        return allergyTypeId;
    }
}
