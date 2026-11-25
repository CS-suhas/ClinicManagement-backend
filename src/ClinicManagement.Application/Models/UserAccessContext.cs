using ClinicManagement.Application.Common;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

/// <summary>
/// Everything authorization needs to know about one signed-in user, loaded once per request.
/// </summary>
/// <param name="PatientId">The linked patient profile; null when there is none.</param>
/// <param name="ProviderId">The linked provider profile; null when there is none.</param>
/// <param name="ProviderStatus">The provider profile's verification status; null when there is no provider profile.</param>
/// <param name="AdministeredClinicIds">Clinics the user administers; empty when none.</param>
public sealed record UserAccessContext(
    Guid UserId,
    bool IsPlatformAdmin,
    Guid? PatientId,
    Guid? ProviderId,
    ProviderVerificationStatus? ProviderStatus,
    IReadOnlyList<Guid> AdministeredClinicIds)
{
    public static UserAccessContext Empty(Guid userId) => new(userId, false, null, null, null, []);

    /// <summary>
    /// The roles this context grants. <paramref name="isConfiguredAdmin"/> adds <see cref="Roles.Admin"/> for a
    /// subject listed in configuration.
    /// </summary>
    public IReadOnlyList<string> DeriveRoles(bool isConfiguredAdmin = false)
    {
        var roles = new List<string>(4);
        if (PatientId is not null)
            roles.Add(Roles.Patient);
        if (ProviderStatus == ProviderVerificationStatus.VERIFIED)
            roles.Add(Roles.Doctor);
        if (AdministeredClinicIds.Count > 0)
            roles.Add(Roles.ClinicAdmin);
        if (IsPlatformAdmin || isConfiguredAdmin)
            roles.Add(Roles.Admin);
        return roles;
    }
}
