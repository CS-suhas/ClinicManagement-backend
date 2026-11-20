using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Clinics.Mapping;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Mapping;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Mapping;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Clinics.Services;

public class ClinicService(
    IClinicRepository clinicRepository,
    IProviderRepository providerRepository,
    IAppointmentRepository appointmentRepository) : IClinicService
{
    public async Task<ClinicResponse> GetClinicByIdAsync(Guid clinicId)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);
        return clinic.ToResponse();
    }

    public async Task<IEnumerable<ClinicResponse>> GetAllClinicsAsync()
    {
        var clinics = await clinicRepository.GetAllClinicsAsync();
        return clinics.Select(clinic => clinic.ToResponse());
    }

    public async Task<PagedResponse<ClinicResponse>> SearchClinicsAsync(PageQuery query)
    {
        var offset = query.GetOffset();
        var (items, totalCount) = await clinicRepository.SearchClinicsAsync(query, offset);
        return new PagedResponse<ClinicResponse>(
            items.Select(item => item.ToResponse()).ToArray(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<List<ProviderResponse>> GetProvidersByClinicIdAsync(Guid clinicId)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        _ = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);

        var providers = await providerRepository.GetProvidersByClinicIdAsync(clinicId);
        return providers.Select(provider => provider.ToResponse()).ToList();
    }

    public async Task<List<AppointmentResponse>> GetAppointmentsByClinicIdAsync(Guid clinicId)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        _ = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);

        var appointments = await appointmentRepository.GetAppointmentsByClinicIdAsync(clinicId);
        return appointments.Select(appointment => appointment.ToResponse()).ToList();
    }

    public async Task<Guid> AddClinicAsync(ClinicRequest request)
    {
        ValidateRequest(request);

        var clinic = request.ToEntity();
        await clinicRepository.AddClinicAsync(clinic);
        return clinic.Id;
    }

    public async Task<Guid> UpdateClinicAsync(ClinicRequest request)
    {
        ValidateRequest(request);

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
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId);
        if (clinic == null)
        {
            throw new ResourceNotFoundException("Clinic", clinicId);
        }
        await clinicRepository.DeleteClinicAsync(clinic.Id);
        return clinicId;
    }

    private static void ValidateRequest(ClinicRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Clinic ID");
        RequestValidator.EnsureText(request.Name, "name", "Clinic name");
    }
}
