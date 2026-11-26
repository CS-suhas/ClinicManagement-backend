using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Mapping;
using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Services;

public class AllergyTypeService(IAllergyTypeRepository allergyTypeRepository) : IAllergyTypeService
{
    public async Task<AllergyTypeResponse> GetAllergyTypeByIdAsync(Guid allergyTypeId)
    {
        RequestValidator.EnsureGuid(allergyTypeId, nameof(allergyTypeId), "Allergy type ID");

        var allergyType = await allergyTypeRepository.GetAllergyTypeByIdAsync(allergyTypeId)
            ?? throw new ResourceNotFoundException("Allergy type", allergyTypeId);
        var counts = await allergyTypeRepository.GetPatientCountsByAllergyTypeAsync();
        return allergyType.ToResponse(counts.GetValueOrDefault(allergyType.Id));
    }

    public async Task<IEnumerable<AllergyTypeResponse>> GetAllAllergyTypesAsync()
    {
        var allergyTypes = (await allergyTypeRepository.GetAllAllergyTypesAsync()).ToList();
        var counts = await allergyTypeRepository.GetPatientCountsByAllergyTypeAsync();

        return allergyTypes.Select(item => item.ToResponse(counts.GetValueOrDefault(item.Id)));
    }

    public async Task<PagedResponse<AllergyTypeResponse>> SearchAllergyTypesAsync(PageQuery query)
    {
        var offset = query.GetOffset();
        var (items, totalCount) = await allergyTypeRepository.SearchAllergyTypesAsync(query, offset);
        var counts = await allergyTypeRepository.GetPatientCountsByAllergyTypeAsync();
        return new PagedResponse<AllergyTypeResponse>(
            items.Select(item => item.ToResponse(counts.GetValueOrDefault(item.Id))).ToArray(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<IEnumerable<PatientResponse>> GetPatientsByAllergyTypeIdAsync(Guid allergyTypeId)
    {
        RequestValidator.EnsureGuid(allergyTypeId, nameof(allergyTypeId), "Allergy type ID");

        if (await allergyTypeRepository.GetAllergyTypeByIdAsync(allergyTypeId) == null)
        {
            throw new ResourceNotFoundException("Allergy type", allergyTypeId);
        }

        var patients = await allergyTypeRepository.GetPatientsByAllergyTypeIdAsync(allergyTypeId);
        return patients.Select(patient => patient.ToResponse());
    }

    public async Task<Guid> AddAllergyTypeAsync(AllergyTypeRequest request)
    {
        ValidateRequest(request);

        var allergyType = request.ToEntity();
        await allergyTypeRepository.AddAllergyTypeAsync(allergyType);
        return allergyType.Id;
    }

    public async Task<Guid> UpdateAllergyTypeAsync(AllergyTypeRequest request)
    {
        ValidateRequest(request);

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
        RequestValidator.EnsureGuid(allergyTypeId, nameof(allergyTypeId), "Allergy type ID");

        var allergyType = await allergyTypeRepository.GetAllergyTypeByIdAsync(allergyTypeId);
        if (allergyType == null)
        {
            throw new ResourceNotFoundException("Allergy type", allergyTypeId);
        }

        if (await allergyTypeRepository.HasPatientsWithAllergyTypeAsync(allergyTypeId))
        {
            throw new AllergyTypeInUseException(allergyTypeId);
        }

        await allergyTypeRepository.DeleteAllergyTypeAsync(allergyType.Id);
        return allergyTypeId;
    }

    private static void ValidateRequest(AllergyTypeRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Allergy type ID");
        RequestValidator.EnsureText(request.Name, "name", "Allergy type name");
    }
}
