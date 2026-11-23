using System.Security.Claims;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Enums;
using Microsoft.AspNetCore.Authentication;

namespace ClinicManagement.Api.Security;

public sealed class RoleClaimsTransformation(
    IConfiguration configuration,
    IPatientRepository patientRepository,
    IProviderRepository providerRepository,
    IClinicAdminRepository clinicAdminRepository,
    IPlatformAdminRepository platformAdminRepository) : IClaimsTransformation
{
    private const string AdminSubjectsKey = "Authorization:AdminSubjects";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            return principal;

        foreach (var claim in identity.FindAll(ClaimTypes.Role).ToList())
            identity.RemoveClaim(claim);

        foreach (var role in await ResolveRolesAsync(subject))
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return principal;
    }

    private async Task<List<string>> ResolveRolesAsync(string subject)
    {
        var roles = new List<string>();

        if (await patientRepository.GetByIdentitySubjectAsync(subject) is not null)
            roles.Add(AuthorizationPolicies.PatientRole);

        var provider = await providerRepository.GetByIdentitySubjectAsync(subject);
        if (provider?.VerificationStatus == ProviderVerificationStatus.VERIFIED)
            roles.Add(AuthorizationPolicies.DoctorRole);

        if (await clinicAdminRepository.IsAnyAdminAsync(subject))
            roles.Add(AuthorizationPolicies.ClinicAdminRole);

        if (IsConfiguredAdmin(subject) || await platformAdminRepository.IsAdminAsync(subject))
            roles.Add(AuthorizationPolicies.AdminRole);

        return roles;
    }

    private bool IsConfiguredAdmin(string subject) =>
        (configuration.GetSection(AdminSubjectsKey).Get<string[]>() ?? [])
            .Contains(subject, StringComparer.Ordinal);
}
