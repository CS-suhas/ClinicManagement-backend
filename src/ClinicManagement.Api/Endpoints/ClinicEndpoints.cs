using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class ClinicEndpoints
{
    public static IEndpointRouteBuilder MapClinicEndpoints(this WebApplication app)
    {
        app.MapGet("/api/clinics", async ([FromServices] IClinicManagementService service) =>
            Results.Ok(await service.GetAllClinicsAsync()));

        app.MapGet("/api/clinics/{id}", async (Guid id, [FromServices] IClinicManagementService service) =>
        {
            var clinic = await service.GetClinicByIdAsync(id);
            return clinic is null ? Results.NotFound() : Results.Ok(clinic);
        });

        app.MapPost("/api/clinics", async ([FromBody] ClinicRequest request, [FromServices] IClinicManagementService service) =>
        {
            var id = await service.AddClinicAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/clinics/{id}", new { id });
        });

        app.MapPut("/api/clinics/{id}", async (Guid id, [FromBody] ClinicRequest request, [FromServices] IClinicManagementService service) =>
        {
            if (await service.GetClinicByIdAsync(id) is null)
                return Results.NotFound();

            await service.UpdateClinicAsync(request with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/clinics/{id}", async (Guid id, [FromServices] IClinicManagementService service) =>
        {
            if (await service.GetClinicByIdAsync(id) is null)
                return Results.NotFound();

            await service.DeleteClinicAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
