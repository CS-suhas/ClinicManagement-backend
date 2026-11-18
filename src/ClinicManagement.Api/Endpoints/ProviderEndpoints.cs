using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(this WebApplication app)
    {
        app.MapGet("/api/providers", async ([FromServices] IProviderManagementService service) =>
            Results.Ok(await service.GetAllProvidersAsync()));

        app.MapGet("/api/providers/{id}", async (Guid id, [FromServices] IProviderManagementService service) =>
        {
            var provider = await service.GetProviderByIdAsync(id);
            return provider is null ? Results.NotFound() : Results.Ok(provider);
        });

        app.MapPost("/api/providers", async ([FromBody] ProviderRequest request, [FromServices] IProviderManagementService service) =>
        {
            try
            {
                var id = await service.AddProviderAsync(request with { Id = Guid.NewGuid() });
                return Results.Created($"/api/providers/{id}", new { id });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        app.MapPut("/api/providers/{id}", async (Guid id, [FromBody] ProviderRequest request, [FromServices] IProviderManagementService service) =>
        {
            if (await service.GetProviderByIdAsync(id) is null)
                return Results.NotFound();

            try
            {
                await service.UpdateProviderAsync(request with { Id = id });
                return Results.NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return Results.BadRequest(exception.Message);
            }
        });

        app.MapDelete("/api/providers/{id}", async (Guid id, [FromServices] IProviderManagementService service) =>
        {
            if (await service.GetProviderByIdAsync(id) is null)
                return Results.NotFound();

            await service.DeleteProviderAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
