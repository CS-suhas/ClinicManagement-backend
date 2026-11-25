using ClinicManagement.Application.Common;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

/// <summary>
/// Everything authorization needs to know about one signed-in user, loaded once per request.
/// </summary>
/// <param name="AssignedRoles">Roles granted explicitly through <c>UserRoles</c>.</param>
/// <param name="PatientId">The linked patient profile; null when there is none.</param>
/// <param name="ProviderId">The linked provider profile; null when there is none.</param>
/// <param name="ProviderStatus">The provider profile's verification status; null when there is no provider profile.</param>
/// <param name="AdministeredClinicIds">Clinics the user administers; empty when none.</param>
public sealed record UserAccessContext(
    Guid UserId,
    IReadOnlyList<string> AssignedRoles,
    Guid? PatientId,
    Guid? ProviderId,
    ProviderVerificationStatus? ProviderStatus,
    IReadOnlyList<Guid> AdministeredClinicIds)
{
    public bool HasAssignedRole(string role) => AssignedRoles.Contains(role, StringComparer.Ordinal);

    public static UserAccessContext Empty(Guid userId) => new(userId, [], null, null, null, []);

    /// <summary>The roles this context grants.</summary>
    public IReadOnlyList<string> DeriveRoles()
    {
        var roles = new List<string>(3 + AssignedRoles.Count);
        if (PatientId is not null)
            roles.Add(Roles.Patient);
        if (ProviderStatus == ProviderVerificationStatus.VERIFIED)
            roles.Add(Roles.Doctor);
        if (AdministeredClinicIds.Count > 0)
            roles.Add(Roles.ClinicAdmin);
        foreach (var role in AssignedRoles)
        {
            if (!roles.Contains(role, StringComparer.Ordinal))
                roles.Add(role);
        }
        return roles;
    }
}
