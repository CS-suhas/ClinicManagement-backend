using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Mapping;
using ClinicManagement.Application.Clinics.Models;

namespace ClinicManagement.Application.Clinics.Services;

public class ClinicService(IClinicRepository clinicRepository) : IClinicService
{
    public async Task<ClinicResponse?> GetClinicByIdAsync(Guid clinicId)
    {
        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId);
        return clinic?.ToResponse();
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
            throw new KeyNotFoundException($"Clinic '{request.Id}' was not found.");
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
            throw new KeyNotFoundException($"Clinic '{clinicId}' was not found.");
        }
        await clinicRepository.DeleteClinicAsync(clinic.Id);
        return clinicId;
    }
}
