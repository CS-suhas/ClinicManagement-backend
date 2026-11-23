using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Api.Security;
using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Api.Endpoints;

public static class ClinicEndpoints
{
    public static IEndpointRouteBuilder MapClinicEndpoints(this WebApplication app)
    {
        // Outside the group: the group's sign-in requirement would apply on top of the toggle.
        // The list carries only clinic contact details and a count, no patient or user data.
        app.MapGet("/api/clinics", async ([FromServices] IClinicService service) =>
            Results.Ok(await service.GetAllClinicsAsync()))
            .WithName("GetClinics")
            .RequireAuthorization(AuthorizationPolicies.PublicDoctors)
            .WithSummary("List clinics")
            .WithDescription("Every clinic (id, name, address, phoneNumber), each with providerCount: the number of verified providers at the clinic. Public while PublicAccess:Doctors is true (the default), for the doctor directory; otherwise signed-in users only.")
            .Produces<List<ClinicListItemResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        var clinics = app.MapGroup("/api/clinics").RequireAuthorization(AuthorizationPolicies.Authenticated);

        clinics.MapGet("/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.SearchClinicsAsync(query)))
            .WithName("SearchClinics")
            .WithSummary("Search clinics")
            .WithDescription("Searches clinic names, addresses, and phone numbers, and returns a page. Each item includes providerCount, the number of verified providers at the clinic.")
            .Produces<PagedResponse<ClinicListItemResponse>>(StatusCodes.Status200OK);

        clinics.MapGet("/{id}", async (Guid id, [FromServices] IClinicService service) =>
            Results.Ok(await service.GetClinicByIdAsync(id)));

        clinics.MapGet("/{id}/summary", async (
            Guid id,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.GetClinicSummaryAsync(id)))
            .WithName("GetClinicSummary")
            .WithSummary("Get a clinic summary")
            .WithDescription("Signed-in users only. Returns the clinic, providerCount (verified providers), todayAppointmentCount (UTC day, declined and cancelled excluded), pendingJoinRequestCount (only for an administrator or an administrator of this clinic, otherwise null), topSpecialties (up to 3 by verified provider count, ties alphabetical) and up to 5 verified providers ordered by last and first name. Returns 404 for an unknown clinic.")
            .Produces<ClinicSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        clinics.MapGet("/{id}/providers", async (
            Guid id,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.GetProvidersByClinicIdAsync(id)))
            .WithName("GetClinicProviders")
            .WithSummary("Get providers at a clinic")
            .Produces<List<ProviderResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        clinics.MapGet("/{id}/appointments", async (
            Guid id,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.GetAppointmentsByClinicIdAsync(id)))
            .WithName("GetClinicAppointments")
            .WithSummary("Get appointments for a clinic")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        clinics.MapPost("", async ([FromBody] ClinicRequest request, [FromServices] IClinicService service) =>
        {
            var id = await service.AddClinicAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/clinics/{id}", new { id });
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        clinics.MapPut("/{id}", async (Guid id, [FromBody] ClinicRequest request, [FromServices] IClinicService service) =>
        {
            await service.UpdateClinicAsync(request with { Id = id });
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        clinics.MapDelete("/{id}", async (Guid id, [FromServices] IClinicService service) =>
        {
            await service.DeleteClinicAsync(id);
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        clinics.MapPost("/{clinicId}/admins", async (
            Guid clinicId,
            [FromBody] ClinicAdminRequest request,
            [FromServices] IClinicService service) =>
        {
            var id = await service.AddClinicAdminAsync(clinicId, request.IdentitySubject);
            return Results.Created($"/api/clinics/{clinicId}/admins", new { id });
        })
            .WithName("AddClinicAdmin")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithSummary("Assign an identity as an administrator of a clinic (admin)")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
