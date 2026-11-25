using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

/// <summary>Explicitly granted roles (the <c>UserRoles</c> table).</summary>
public interface IUserRoleRepository
{
    /// <summary>Grants <paramref name="role"/>. Returns false when the user already holds it.</summary>
    /// <param name="grantedByUserId">The administrator making the grant; null for configuration.</param>
    Task<bool> AssignAsync(Guid userId, string role, Guid? grantedByUserId);

    /// <summary>Revokes <paramref name="role"/>. Returns false when the user did not hold it.</summary>
    Task<bool> RevokeAsync(Guid userId, string role);

    /// <summary>Every grant, ordered by role and then by user subject.</summary>
    Task<IReadOnlyList<UserRoleAssignment>> ListAsync();
}
