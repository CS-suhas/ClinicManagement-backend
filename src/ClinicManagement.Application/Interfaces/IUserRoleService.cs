using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

/// <summary>Administration of explicitly granted roles. Callers must already be authorized to manage roles.</summary>
public interface IUserRoleService
{
    Task<IReadOnlyList<UserRoleAssignment>> ListAssignmentsAsync();

    /// <summary>The user's assigned and effective roles. Throws when no user has <paramref name="identitySubject"/>.</summary>
    Task<UserRolesResponse> GetUserRolesAsync(string identitySubject);

    /// <summary>
    /// Grants an assignable role. The user is created when they have not signed in yet. Granting a role the user
    /// already holds changes nothing.
    /// </summary>
    Task AssignRoleAsync(string identitySubject, string role);

    /// <summary>Revokes an assignable role. Revoking a role the user does not hold changes nothing.</summary>
    Task RevokeRoleAsync(string identitySubject, string role);
}
