using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Clinics.Mapping;
using ClinicManagement.Application.Clinics.Models;

namespace ClinicManagement.Application.Clinics.Services;

public class ClinicService(IClinicRepository clinicRepository) : IClinicService
{
    public async Task<ClinicResponse> GetClinicByIdAsync(Guid clinicId)
    {
        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);
        return clinic.ToResponse();
    }

    public async Task<IEnumerable<ClinicResponse>> GetAllClinicsAsync()
    {
        var clinics = await clinicRepository.GetAllClinicsAsync();
        return clinics.Select(clinic => clinic.ToResponse());
    }

    public async Task<Guid> AddClinicAsync(ClinicRequest request)
    {
        var clinic = request.ToEntity();
        await clinicRepository.AddClinicAsync(clinic);
        return clinic.Id;
    }

    public async Task<Guid> UpdateClinicAsync(ClinicRequest request)
    {
        var clinic = await clinicRepository.GetClinicByIdAsync(request.Id);
        if (clinic == null)
        {
            throw new ResourceNotFoundException("Clinic", request.Id);
        }
        request.ApplyTo(clinic);
        await clinicRepository.UpdateClinicAsync(clinic);
        return clinic.Id;
    }

    public async Task<Guid> DeleteClinicAsync(Guid clinicId)
    {
        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId);
        if (clinic == null)
        {
            throw new ResourceNotFoundException("Clinic", clinicId);
        }
        await clinicRepository.DeleteClinicAsync(clinic.Id);
        return clinicId;
    }
}
