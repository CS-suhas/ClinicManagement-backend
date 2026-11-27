namespace ClinicManagement.Application.Models;

public sealed record UserRoleAssignment(
    Guid UserId,
    string IdentitySubject,
    string? Email,
    string? DisplayName,
    string Role,
    DateTimeOffset GrantedAtUtc,
    Guid? GrantedByUserId);

public sealed record UserRolesResponse(
    Guid UserId,
    string IdentitySubject,
    IReadOnlyList<string> AssignedRoles,
    IReadOnlyList<string> EffectiveRoles);
