using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.AllergyTypes.Interfaces;
using ClinicManagement.Application.AllergyTypes.Mapping;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Patients.Mapping;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.AllergyTypes.Services;

public class AllergyTypeService(IAllergyTypeRepository allergyTypeRepository) : IAllergyTypeService
{
    public async Task<AllergyTypeResponse> GetAllergyTypeByIdAsync(Guid allergyTypeId)
    {
        RequestValidator.EnsureGuid(allergyTypeId, nameof(allergyTypeId), "Allergy type ID");

        var allergyType = await allergyTypeRepository.GetAllergyTypeByIdAsync(allergyTypeId)
            ?? throw new ResourceNotFoundException("Allergy type", allergyTypeId);
        return allergyType.ToResponse();
    }

    public async Task<IEnumerable<AllergyTypeResponse>> GetAllAllergyTypesAsync()
    {
        var allergyTypes = await allergyTypeRepository.GetAllAllergyTypesAsync();

        return allergyTypes.Select(item => item.ToResponse());
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
