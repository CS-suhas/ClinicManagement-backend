using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Allergies.Models;
using ClinicManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class AllergyEndpoints
{
    public static IEndpointRouteBuilder MapAllergyEndpoints(this WebApplication app)
    {
        app.MapGet("/api/allergies", async ([FromServices] IAllergyManagementService service) =>
            Results.Ok(await service.GetAllAllergiesAsync()));

        app.MapGet("/api/allergies/{id}", async (Guid id, [FromServices] IAllergyManagementService service) =>
        {
            var allergy = await service.GetAllergyByIdAsync(id);
            return allergy is null ? Results.NotFound() : Results.Ok(allergy);
        });

        app.MapPost("/api/allergies", async ([FromBody] AllergyRequest request, [FromServices] IAllergyManagementService service) =>
        {
            try
            {
                var id = await service.AddAllergyAsync(request with { Id = Guid.NewGuid() });
                return Results.Created($"/api/allergies/{id}", new { id });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.BadRequest(exception.Message);
            }
            catch (DuplicatePatientAllergyException exception)
            {
                return Results.Conflict(exception.Message);
            }
        });

        app.MapPut("/api/allergies/{id}", async (Guid id, [FromBody] AllergyRequest request, [FromServices] IAllergyManagementService service) =>
        {
            if (await service.GetAllergyByIdAsync(id) is null)
                return Results.NotFound();

            try
            {
                await service.UpdateAllergyAsync(request with { Id = id });
                return Results.NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return Results.BadRequest(exception.Message);
            }
            catch (DuplicatePatientAllergyException exception)
            {
                return Results.Conflict(exception.Message);
            }
        });

        app.MapDelete("/api/allergies/{id}", async (Guid id, [FromServices] IAllergyManagementService service) =>
        {
            if (await service.GetAllergyByIdAsync(id) is null)
                return Results.NotFound();

            await service.DeleteAllergyAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
