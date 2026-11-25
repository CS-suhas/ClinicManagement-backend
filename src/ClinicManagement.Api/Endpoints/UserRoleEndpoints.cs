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
            .WithTags(ApiTags.UserRoles)
            .WithName("ListUserRoles")
            .WithSummary("List explicitly assigned roles (admin)")
            .WithDescription("Administrators only. Every role granted through UserRoles, with the user it belongs to and who granted it (grantedByUserId is null for grants made by configuration or migration). Roles derived from data (patient, doctor, clinicadmin) are not listed.")
            .RequirePermission(Permissions.UserRolesManage)
            .Produces<IReadOnlyList<UserRoleAssignment>>(StatusCodes.Status200OK)
            .ProducesAuthFailures();

        var roles = app.MapGroup("/api/users/{identitySubject}/roles")
            .WithTags(ApiTags.UserRoles)
            .RequirePermission(Permissions.UserRolesManage);

        roles.MapGet("", async (string identitySubject, [FromServices] IUserRoleService service) =>
            Results.Ok(await service.GetUserRolesAsync(identitySubject)))
            .WithName("GetUserRoles")
            .WithSummary("Get a user's roles (admin)")
            .WithDescription("Administrators only. assignedRoles are the roles granted explicitly; effectiveRoles adds the roles derived from the user's data. Returns 404 when no user has the identity subject.")
            .Produces<UserRolesResponse>(StatusCodes.Status200OK)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        roles.MapPut("/{role}", async (string identitySubject, string role, [FromServices] IUserRoleService service) =>
        {
            await service.AssignRoleAsync(identitySubject, role);
            return Results.NoContent();
        })
            .WithName("AssignUserRole")
            .WithSummary("Assign a role to a user (admin)")
            .WithDescription("Administrators only. Idempotent. Creates the user when they have not signed in yet. Only assignable roles are accepted (currently: admin); derived roles return 400.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesAuthFailures();

        roles.MapDelete("/{role}", async (string identitySubject, string role, [FromServices] IUserRoleService service) =>
        {
            await service.RevokeRoleAsync(identitySubject, role);
            return Results.NoContent();
        })
            .WithName("RevokeUserRole")
            .WithSummary("Revoke a role from a user (admin)")
            .WithDescription("Administrators only. Idempotent. Only assignable roles are accepted (400 otherwise). Returns 404 when no user has the identity subject, and 409 when an administrator revokes their own admin role, or when the subject is listed in Authorization:AdminSubjects.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
