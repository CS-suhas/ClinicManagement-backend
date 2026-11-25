using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Services;

public sealed class UserRoleService(
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    ICurrentUser currentUser,
    AuthorizationSettings settings) : IUserRoleService
{
    public Task<IReadOnlyList<UserRoleAssignment>> ListAssignmentsAsync() => userRoleRepository.ListAsync();

    public async Task<UserRolesResponse> GetUserRolesAsync(string identitySubject)
    {
        var user = await GetRequiredUserAsync(identitySubject);
        var context = await userRepository.GetAccessContextAsync(user.Id);
        return new UserRolesResponse(user.Id, user.IdentitySubject, context.AssignedRoles, context.DeriveRoles());
    }

    public async Task AssignRoleAsync(string identitySubject, string role)
    {
        RequestValidator.EnsureText(identitySubject, nameof(identitySubject), "Identity subject");
        EnsureAssignable(role);

        // As with clinic admins, the person may not have signed in yet; their profile fills in on first sign-in.
        var user = await userRepository.GetOrCreateAsync(identitySubject.Trim());
        await userRoleRepository.AssignAsync(user.Id, role, currentUser.UserId);
    }

    public async Task RevokeRoleAsync(string identitySubject, string role)
    {
        EnsureAssignable(role);
        var user = await GetRequiredUserAsync(identitySubject);

        if (role == Roles.Admin)
        {
            if (user.Id == currentUser.UserId)
                throw new RoleRevocationNotAllowedException(user.IdentitySubject, role, "administrators cannot revoke their own admin role.");
            if (settings.IsConfiguredAdmin(user.IdentitySubject))
                throw new RoleRevocationNotAllowedException(user.IdentitySubject, role, "the subject is listed in Authorization:AdminSubjects; remove it there first.");
        }

        await userRoleRepository.RevokeAsync(user.Id, role);
    }

    private async Task<Domain.Entities.User> GetRequiredUserAsync(string identitySubject)
    {
        RequestValidator.EnsureText(identitySubject, nameof(identitySubject), "Identity subject");
        return await userRepository.GetBySubjectAsync(identitySubject.Trim())
            ?? throw new ResourceNotFoundException($"User with identity subject '{identitySubject.Trim()}' was not found.");
    }

    private static void EnsureAssignable(string role)
    {
        if (Roles.Assignable.Contains(role))
            return;

        var reason = Roles.Derived.Contains(role)
            ? $"Role '{role}' is derived from the user's data and cannot be assigned or revoked directly."
            : $"Role '{role}' is not an assignable role. Assignable roles: {string.Join(", ", Roles.Assignable.Order())}.";
        throw new DomainValidationException(reason, nameof(role));
    }
}
