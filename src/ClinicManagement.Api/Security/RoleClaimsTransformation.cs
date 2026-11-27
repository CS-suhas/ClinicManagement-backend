using System.Security.Claims;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using Microsoft.AspNetCore.Authentication;

namespace ClinicManagement.Api.Security;

public sealed class RoleClaimsTransformation(
    AuthorizationSettings settings,
    IUserRepository userRepository,
    IUserRoleRepository userRoleRepository,
    UserAccessContextHolder holder) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
            return principal;

        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            return principal;

        var context = holder.Subject == subject ? holder.Context : null;
        if (context is null)
        {
            context = await LoadAccessContextAsync(principal, subject);
            holder.Set(subject, context);
        }

        var transformed = identity.Clone();
        foreach (var claim in transformed.FindAll(ClaimTypes.Role).Concat(transformed.FindAll(ClinicClaimTypes.UserId)).ToList())
            transformed.RemoveClaim(claim);

        transformed.AddClaim(new Claim(ClinicClaimTypes.UserId, context.UserId.ToString()));
        foreach (var role in context.DeriveRoles())
            transformed.AddClaim(new Claim(ClaimTypes.Role, role));

        return new ClaimsPrincipal(transformed);
    }

    private async Task<UserAccessContext> LoadAccessContextAsync(ClaimsPrincipal principal, string subject)
    {
        var user = await userRepository.GetOrCreateAsync(
            subject,
            principal.FindFirst("email")?.Value,
            principal.FindFirst("name")?.Value ?? principal.FindFirst("preferred_username")?.Value);
        var context = await userRepository.GetAccessContextAsync(user.Id);

        if (!context.HasAssignedRole(Roles.Admin) && settings.IsConfiguredAdmin(subject))
        {
            await userRoleRepository.AssignAsync(user.Id, Roles.Admin, grantedByUserId: null);
            context = context with { AssignedRoles = [.. context.AssignedRoles, Roles.Admin] };
        }

        return context;
    }
}
