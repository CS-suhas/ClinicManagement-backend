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
