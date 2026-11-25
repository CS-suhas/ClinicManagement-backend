using ClinicManagement.Api.Security;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class UserRoleEndpoints
{
    public static IEndpointRouteBuilder MapUserRoleEndpoints(this WebApplication app)
    {
        app.MapGet("/api/user-roles", async ([FromServices] IUserRoleService service) =>
            Results.Ok(await service.ListAssignmentsAsync()))
            .WithName("ListUserRoles")
            .RequirePermission(Permissions.UserRolesManage)
            .WithSummary("List explicitly assigned roles (admin)")
            .WithDescription("Every role granted through UserRoles, with the user it belongs to and who granted it (grantedByUserId is null for grants made by configuration or migration). Roles derived from data (patient, doctor, clinicadmin) are not listed.")
            .Produces<IReadOnlyList<UserRoleAssignment>>(StatusCodes.Status200OK);

        var roles = app.MapGroup("/api/users/{identitySubject}/roles")
            .RequirePermission(Permissions.UserRolesManage);

        roles.MapGet("", async (string identitySubject, [FromServices] IUserRoleService service) =>
            Results.Ok(await service.GetUserRolesAsync(identitySubject)))
            .WithName("GetUserRoles")
            .WithSummary("Get a user's roles (admin)")
            .WithDescription("assignedRoles are the roles granted explicitly; effectiveRoles adds the roles derived from the user's data. Returns 404 when no user has the identity subject.")
            .Produces<UserRolesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        roles.MapPut("/{role}", async (string identitySubject, string role, [FromServices] IUserRoleService service) =>
        {
            await service.AssignRoleAsync(identitySubject, role);
            return Results.NoContent();
        })
            .WithName("AssignUserRole")
            .WithSummary("Assign a role to a user (admin)")
            .WithDescription("Idempotent. Creates the user when they have not signed in yet. Only assignable roles are accepted (currently: admin); derived roles return 400.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest);

        roles.MapDelete("/{role}", async (string identitySubject, string role, [FromServices] IUserRoleService service) =>
        {
            await service.RevokeRoleAsync(identitySubject, role);
            return Results.NoContent();
        })
            .WithName("RevokeUserRole")
            .WithSummary("Revoke a role from a user (admin)")
            .WithDescription("Idempotent. Returns 409 when an administrator revokes their own admin role, or when the subject is listed in Authorization:AdminSubjects.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
