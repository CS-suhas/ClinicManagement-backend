using ClinicManagement.Application.Common;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

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
