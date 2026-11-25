namespace ClinicManagement.Application.Models;

/// <summary>One explicitly granted role.</summary>
/// <param name="GrantedByUserId">The administrator who granted it; null when granted by configuration or migration.</param>
public sealed record UserRoleAssignment(
    Guid UserId,
    string IdentitySubject,
    string? Email,
    string? DisplayName,
    string Role,
    DateTimeOffset GrantedAtUtc,
    Guid? GrantedByUserId);

/// <summary>A user's roles: those granted explicitly, and every role they currently hold.</summary>
/// <param name="AssignedRoles">Roles in <c>UserRoles</c>, which an administrator can revoke.</param>
/// <param name="EffectiveRoles">Assigned roles plus the roles derived from the user's data.</param>
public sealed record UserRolesResponse(
    Guid UserId,
    string IdentitySubject,
    IReadOnlyList<string> AssignedRoles,
    IReadOnlyList<string> EffectiveRoles);
