using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Mapping;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Services;

public class MeService(
    IPatientRepository patientRepository,
    ICurrentUser currentUser) : IMeService
{
    private const string PatientRole = "patient";

    public async Task<MeResponse> GetCurrentUserAsync()
    {
        CurrentUserGuard.RequireAuthenticated(currentUser);

        var patient = await currentUser.GetPatientAsync();
        var provider = await currentUser.GetProviderAsync();

        return new MeResponse(
            currentUser.Subject,
            currentUser.Roles,
            patient is null ? null : new MeLinkedPatient(patient.Id),
            provider is null ? null : new MeLinkedProvider(provider.Id, provider.VerificationStatus.ToString()));
    }

    public async Task<Guid> CreatePatientProfileAsync(PatientRequest request)
    {
        CurrentUserGuard.RequireAuthenticated(currentUser);
        var subject = CurrentUserGuard.RequireSubject(currentUser);

        if (!currentUser.IsInRole(PatientRole))
            throw new ForbiddenException("The signed-in user does not have the patient role.");
        if (await currentUser.GetPatientAsync() is not null)
            throw new IdentityAlreadyLinkedException("patient", subject);

        var patient = (request with { Id = Guid.NewGuid() }).ToEntity();
        patient.AssignIdentitySubject(subject);

        await patientRepository.AddPatientAsync(patient);
        return patient.Id;
    }
}
