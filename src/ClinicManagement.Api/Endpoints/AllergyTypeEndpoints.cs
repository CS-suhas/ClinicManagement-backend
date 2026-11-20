using ClinicManagement.Application.AllergyTypes.Interfaces;
using ClinicManagement.Application.AllergyTypes.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class AllergyTypeEndpoints
{
    public static IEndpointRouteBuilder MapAllergyTypeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/allergy-types", async ([FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetAllAllergyTypesAsync()));

        app.MapGet("/api/allergy-types/{id}", async (Guid id, [FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetAllergyTypeByIdAsync(id)));

        app.MapPost("/api/allergy-types", async ([FromBody] AllergyTypeRequest request, [FromServices] IAllergyTypeService service) =>
        {
            var id = await service.AddAllergyTypeAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/allergy-types/{id}", new { id });
        });

        app.MapPut("/api/allergy-types/{id}", async (Guid id, [FromBody] AllergyTypeRequest request, [FromServices] IAllergyTypeService service) =>
        {
            await service.UpdateAllergyTypeAsync(request with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/allergy-types/{id}", async (Guid id, [FromServices] IAllergyTypeService service) =>
        {
            await service.DeleteAllergyTypeAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
