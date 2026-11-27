using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Mapping;
using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Services;

public class ClinicService(
    IClinicRepository clinicRepository,
    IProviderRepository providerRepository,
    IAppointmentRepository appointmentRepository,
    IClinicAdminRepository clinicAdminRepository,
    IUserRepository userRepository,
    IStatusRepository statusRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IClinicService
{
    public async Task<ClinicResponse> GetClinicByIdAsync(Guid clinicId)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);
        return clinic.ToResponse();
    }

    public async Task<IEnumerable<ClinicListItemResponse>> GetAllClinicsAsync()
    {
        var clinics = await clinicRepository.GetAllClinicsAsync();
        var providerCounts = await providerRepository.GetVerifiedProviderCountsByClinicAsync();
        return clinics.Select(clinic => clinic.ToListItem(providerCounts.GetValueOrDefault(clinic.Id))).ToList();
    }

    public async Task<PagedResponse<ClinicListItemResponse>> SearchClinicsAsync(PageQuery query)
    {
        var offset = query.GetOffset();
        var (items, totalCount) = await clinicRepository.SearchClinicsAsync(query, offset);
        var providerCounts = await providerRepository.GetVerifiedProviderCountsByClinicAsync();
        return new PagedResponse<ClinicListItemResponse>(
            items.Select(item => item.ToListItem(providerCounts.GetValueOrDefault(item.Id))).ToArray(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<ClinicSummaryResponse> GetClinicSummaryAsync(Guid clinicId)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");
        currentUser.RequireAuthenticated();

        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);

        var verifiedProviders = (await providerRepository.GetProvidersByClinicIdAsync(clinicId))
            .Where(provider => provider.VerificationStatus == ProviderVerificationStatus.VERIFIED)
            .OrderBy(provider => provider.LastName, StringComparer.Ordinal)
            .ThenBy(provider => provider.FirstName, StringComparer.Ordinal)
            .ToList();

        var todayStart = new DateTimeOffset(timeProvider.GetUtcNow().UtcDateTime.Date, TimeSpan.Zero);
        var todayCounts = await statusRepository.GetClinicCountsAsync(
            [clinicId],
            todayStart,
            todayStart.AddDays(1));

        var topSpecialties = verifiedProviders
            .GroupBy(provider => provider.Specialty, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(TopSpecialtyCount)
            .Select(group => group.Key)
            .ToList();

        return new ClinicSummaryResponse(
            clinic.ToResponse(),
            verifiedProviders.Count,
            todayCounts.TodayAppointmentCount,
            topSpecialties,
            verifiedProviders.Take(ProviderPreviewCount).Select(provider => provider.ToResponse()).ToList());
    }

    private const int TopSpecialtyCount = 3;
    private const int ProviderPreviewCount = 5;

    public async Task<List<ProviderResponse>> GetProvidersByClinicIdAsync(Guid clinicId)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        _ = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);

        var providers = await providerRepository.GetProvidersByClinicIdAsync(clinicId);
        var seesApplicants = currentUser.IsAdministrator() || await currentUser.IsClinicAdminOfAsync(clinicId);
        return providers
            .Where(provider => seesApplicants || provider.VerificationStatus == ProviderVerificationStatus.VERIFIED)
            .Select(provider => provider.ToResponse())
            .ToList();
    }

    public async Task<List<AppointmentResponse>> GetAppointmentsByClinicIdAsync(Guid clinicId)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");
        await currentUser.RequireClinicAdminOrAdministratorAsync(clinicId);

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

    public async Task<Guid> AddClinicAdminAsync(Guid clinicId, string identitySubject)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        _ = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);

        RequestValidator.EnsureText(identitySubject, nameof(identitySubject), "Identity subject");

        var user = await userRepository.GetOrCreateAsync(identitySubject.Trim());
        if (await clinicAdminRepository.IsAdminOfClinicAsync(user.Id, clinicId))
            throw new ClinicAdminAlreadyAssignedException(user.IdentitySubject, clinicId);

        var clinicAdmin = ClinicAdmin.Create(Guid.NewGuid(), clinicId, user.Id);

        await clinicAdminRepository.AddAsync(clinicAdmin);
        return clinicAdmin.Id;
    }

    private static void ValidateRequest(ClinicRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Clinic ID");
        RequestValidator.EnsureText(request.Name, "name", "Clinic name");
    }
}
