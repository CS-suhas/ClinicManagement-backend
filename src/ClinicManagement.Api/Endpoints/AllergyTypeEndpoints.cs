using ClinicManagement.Application.AllergyTypes.Interfaces;
using ClinicManagement.Application.AllergyTypes.Models;
using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Application.Models;
using ClinicManagement.Api.Security;

namespace ClinicManagement.Api.Endpoints;

public static class AllergyTypeEndpoints
{
    public static IEndpointRouteBuilder MapAllergyTypeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/allergy-types", async ([FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetAllAllergyTypesAsync()))
            .WithSummary("List allergy types")
            .WithDescription("Returns every allergy type. Each item carries patientCount, the number of patients with that allergy recorded.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        app.MapGet("/api/allergy-types/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.SearchAllergyTypesAsync(query)))
            .WithName("SearchAllergyTypes")
            .WithSummary("Search allergy types")
            .WithDescription("Searches allergy type names and returns a page. Each item carries patientCount, the number of patients with that allergy recorded.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PagedResponse<AllergyTypeResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/allergy-types/{id}", async (Guid id, [FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetAllergyTypeByIdAsync(id)))
            .WithSummary("Get an allergy type")
            .WithDescription("Returns one allergy type with patientCount, the number of patients with that allergy recorded.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        app.MapGet("/api/allergy-types/{id}/patients", async (Guid id, [FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetPatientsByAllergyTypeIdAsync(id)))
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        app.MapPost("/api/allergy-types", async ([FromBody] AllergyTypeRequest request, [FromServices] IAllergyTypeService service) =>
        {
            var id = await service.AddAllergyTypeAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/allergy-types/{id}", new { id });
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        app.MapPut("/api/allergy-types/{id}", async (Guid id, [FromBody] AllergyTypeRequest request, [FromServices] IAllergyTypeService service) =>
        {
            await service.UpdateAllergyTypeAsync(request with { Id = id });
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        app.MapDelete("/api/allergy-types/{id}", async (Guid id, [FromServices] IAllergyTypeService service) =>
        {
            await service.DeleteAllergyTypeAsync(id);
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return app;
    }
}
