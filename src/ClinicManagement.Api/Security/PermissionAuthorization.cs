using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagement.Api.Security;

/// <summary>Met when one of the signed-in user's roles holds <see cref="Permission"/>.</summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.FindAll(ClaimTypes.Role).Any(role => RolePermissions.Grants(role.Value, requirement.Permission)))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public static class PermissionEndpointExtensions
{
    /// <summary>Requires a signed-in user whose roles hold <paramref name="permission"/>.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(permission);
}
