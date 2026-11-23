using System.Security.Claims;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Enums;
using Microsoft.AspNetCore.Authentication;

namespace ClinicManagement.Api.Security;

/// <summary>
/// Resolves the signed-in identity to a <c>Users</c> row (creating it on first sign-in) and replaces any
/// inbound role claims with roles derived from that user's data.
/// </summary>
public sealed class RoleClaimsTransformation(
    IConfiguration configuration,
    IUserRepository userRepository) : IClaimsTransformation
{
    private const string AdminSubjectsKey = "Authorization:AdminSubjects";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            return principal;

        foreach (var claim in identity.FindAll(ClaimTypes.Role).Concat(identity.FindAll(ClinicClaimTypes.UserId)).ToList())
            identity.RemoveClaim(claim);

        var user = await userRepository.GetOrCreateAsync(
            subject,
            principal.FindFirst("email")?.Value,
            principal.FindFirst("name")?.Value ?? principal.FindFirst("preferred_username")?.Value);
        identity.AddClaim(new Claim(ClinicClaimTypes.UserId, user.Id.ToString()));

        var facts = await userRepository.GetRoleFactsAsync(user.Id);

        if (facts.HasPatientProfile)
            identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationPolicies.PatientRole));
        if (facts.ProviderStatus == ProviderVerificationStatus.VERIFIED)
            identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationPolicies.DoctorRole));
        if (facts.IsClinicAdmin)
            identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationPolicies.ClinicAdminRole));
        if (facts.IsPlatformAdmin || IsConfiguredAdmin(subject))
            identity.AddClaim(new Claim(ClaimTypes.Role, AuthorizationPolicies.AdminRole));

        return principal;
    }

    private bool IsConfiguredAdmin(string subject) =>
        (configuration.GetSection(AdminSubjectsKey).Get<string[]>() ?? [])
            .Contains(subject, StringComparer.Ordinal);
}
