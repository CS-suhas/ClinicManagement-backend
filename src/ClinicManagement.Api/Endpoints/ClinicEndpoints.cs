using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Providers.Models;
using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Api.Endpoints;

public static class ClinicEndpoints
{
    public static IEndpointRouteBuilder MapClinicEndpoints(this WebApplication app)
    {
        app.MapGet("/api/clinics", async ([FromServices] IClinicService service) =>
            Results.Ok(await service.GetAllClinicsAsync()));

        app.MapGet("/api/clinics/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.SearchClinicsAsync(query)))
            .WithName("SearchClinics")
            .WithSummary("Search clinics")
            .WithDescription("Searches clinic names, addresses, and phone numbers, and returns a page.")
            .Produces<PagedResponse<ClinicResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/clinics/{id}", async (Guid id, [FromServices] IClinicService service) =>
            Results.Ok(await service.GetClinicByIdAsync(id)));

        app.MapGet("/api/clinics/{id}/providers", async (
            Guid id,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.GetProvidersByClinicIdAsync(id)))
            .WithName("GetClinicProviders")
            .WithSummary("Get providers at a clinic")
            .Produces<List<ProviderResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/clinics/{id}/appointments", async (
            Guid id,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.GetAppointmentsByClinicIdAsync(id)))
            .WithName("GetClinicAppointments")
            .WithSummary("Get appointments for a clinic")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clinics", async ([FromBody] ClinicRequest request, [FromServices] IClinicService service) =>
        {
            var id = await service.AddClinicAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/clinics/{id}", new { id });
        });

        app.MapPut("/api/clinics/{id}", async (Guid id, [FromBody] ClinicRequest request, [FromServices] IClinicService service) =>
        {
            await service.UpdateClinicAsync(request with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/clinics/{id}", async (Guid id, [FromServices] IClinicService service) =>
        {
            await service.DeleteClinicAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
