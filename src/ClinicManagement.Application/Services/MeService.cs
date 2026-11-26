using ClinicManagement.Application.Common;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Mapping;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Services;

public class MeService(
    IPatientRepository patientRepository,
    ICurrentUser currentUser) : IMeService
{
    public async Task<MeResponse> GetCurrentUserAsync()
    {
        currentUser.RequireAuthenticated();

        var patient = await currentUser.GetPatientAsync();
        var provider = await currentUser.GetProviderAsync();
        var clinicAdminOf = await currentUser.GetAdministeredClinicIdsAsync();

        return new MeResponse(
            currentUser.Subject,
            currentUser.Roles,
            patient is null ? null : new MeLinkedPatient(patient.Id),
            provider is null ? null : new MeLinkedProvider(provider.Id, provider.VerificationStatus.ToString(), provider.ClinicId),
            currentUser.DisplayName,
            clinicAdminOf);
    }

    public async Task<Guid> CreatePatientProfileAsync(PatientRequest request)
    {
        var userId = currentUser.RequireUserId();
        var subject = currentUser.RequireSubject();

        if (await currentUser.GetPatientAsync() is not null)
            throw new IdentityAlreadyLinkedException("patient", subject);

        var patient = (request with { Id = Guid.NewGuid() }).ToEntity();
        patient.AssignUser(userId);

        await patientRepository.AddPatientAsync(patient);
        return patient.Id;
    }
}
