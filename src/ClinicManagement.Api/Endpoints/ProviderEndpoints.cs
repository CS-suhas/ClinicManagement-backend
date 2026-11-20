using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(this WebApplication app)
    {
        app.MapGet("/api/providers", async ([FromServices] IProviderService service) =>
            Results.Ok(await service.GetAllProvidersAsync()));

        app.MapGet("/api/providers/{id}", async (Guid id, [FromServices] IProviderService service) =>
            Results.Ok(await service.GetProviderByIdAsync(id)));

        app.MapGet("/api/providers/{id}/clinic", async (
            Guid id,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetClinicByProviderIdAsync(id)))
            .WithName("GetProviderClinic")
            .WithSummary("Get a provider's clinic")
            .Produces<ClinicResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/providers/{id}/appointments", async (
            Guid id,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetAppointmentsByProviderIdAsync(id)))
            .WithName("GetProviderAppointments")
            .WithSummary("Get appointments for a provider")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/providers", async ([FromBody] ProviderRequest request, [FromServices] IProviderService service) =>
        {
            var id = await service.AddProviderAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/providers/{id}", new { id });
        });

        app.MapPut("/api/providers/{id}", async (Guid id, [FromBody] ProviderRequest request, [FromServices] IProviderService service) =>
        {
            await service.UpdateProviderAsync(request with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/providers/{id}", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.DeleteProviderAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
