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
        var me = app.MapGroup("/api/me")
            .WithTags(ApiTags.Me)
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        me.MapGet("", async ([FromServices] IMeService service) =>
            Results.Ok(await service.GetCurrentUserAsync()))
            .WithName("GetCurrentUser")
            .WithSummary("Get the signed-in user")
            .WithDescription("Returns the token subject, display name, roles, the clinics the caller administers, and the linked patient and provider, or nulls when none exist. provider.clinicId is the provider's clinic, or null while the provider has none.")
            .Produces<MeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        me.MapGet("/dashboard", async ([FromServices] IMeDashboardService service) =>
            Results.Ok(await service.GetDashboardAsync()))
            .WithName("GetMyDashboard")
            .WithSummary("Get the signed-in user's dashboard")
            .WithDescription("Returns one section per role the caller holds: admin (platform counts), clinicAdmin (counts for the caller's clinics), doctor (today, this week in UTC, the next appointment, and pendingRequestCount: REQUESTED appointments starting now or later), and patient (next appointment, upcoming count, and recentDecisions: up to five CONFIRMED or DECLINED appointments, newest decision first). Sections for roles the caller does not hold are null.")
            .Produces<MeDashboardResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        me.MapPost("/patient", async ([FromBody] PatientRequest request, [FromServices] IMeService service) =>
        {
            var id = await service.CreatePatientProfileAsync(request);
            return Results.Created($"/api/patients/{id}", new CreatedResponse(id));
        })
            .WithName("CreateMyPatientProfile")
            .WithSummary("Create the signed-in user's patient profile")
            .WithDescription("Creates a patient linked to the caller's identity; the id in the body is ignored. firstName, lastName, dateOfBirth, phoneNumber and a valid email are required. Returns 409 when the identity already has a patient.")
            .ProducesCreated()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
