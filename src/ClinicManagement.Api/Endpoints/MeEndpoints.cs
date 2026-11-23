using ClinicManagement.Api.Security;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/me", async ([FromServices] IMeService service) =>
            Results.Ok(await service.GetCurrentUserAsync()))
            .WithName("GetCurrentUser")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get the signed-in user")
            .WithDescription("Returns the token subject, roles, and the linked patient and provider, or nulls when none exist.")
            .Produces<MeResponse>(StatusCodes.Status200OK);

        app.MapGet("/api/me/dashboard", async ([FromServices] IMeDashboardService service) =>
            Results.Ok(await service.GetDashboardAsync()))
            .WithName("GetMyDashboard")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get the role-aware dashboard for the signed-in user")
            .WithDescription("Returns one section per role the caller holds: admin (platform counts), clinicAdmin (counts for the caller's clinics plus pending join requests), doctor (today, this week in UTC, and the next appointment), and patient (next appointment and upcoming count). Sections for roles the caller does not hold are null.")
            .Produces<MeDashboardResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/api/me/patient", async (
            [FromBody] PatientRequest request,
            [FromServices] IMeService service) =>
        {
            var id = await service.CreatePatientProfileAsync(request);
            return Results.Created($"/api/patients/{id}", new { id });
        })
            .WithName("CreateMyPatientProfile")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Create the signed-in user's patient profile")
            .WithDescription("Links a new patient to the caller's identity. Returns 409 when the identity already has a patient.")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
