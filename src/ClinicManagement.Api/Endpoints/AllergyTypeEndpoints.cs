using ClinicManagement.Api.Security;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class AllergyTypeEndpoints
{
    public static IEndpointRouteBuilder MapAllergyTypeEndpoints(this WebApplication app)
    {
        var allergyTypes = app.MapGroup("/api/allergy-types").WithTags(ApiTags.AllergyTypes);

        // Queries

        allergyTypes.MapGet("", async ([FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetAllAllergyTypesAsync()))
            .WithName("GetAllergyTypes")
            .WithSummary("List allergy types")
            .WithDescription("Signed-in users. Returns every allergy type. Each item carries patientCount, the number of patients with that allergy recorded.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<List<AllergyTypeResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        allergyTypes.MapGet("/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.SearchAllergyTypesAsync(query)))
            .WithName("SearchAllergyTypes")
            .WithSummary("Search allergy types")
            .WithDescription("Signed-in users. Searches allergy type names and returns a page (page from 1, pageSize 1 to 100, default 20). Each item carries patientCount, the number of patients with that allergy recorded.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PagedResponse<AllergyTypeResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        allergyTypes.MapGet("/{id}", async (Guid id, [FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetAllergyTypeByIdAsync(id)))
            .WithName("GetAllergyType")
            .WithSummary("Get an allergy type")
            .WithDescription("Signed-in users. Returns one allergy type with patientCount, the number of patients with that allergy recorded.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<AllergyTypeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        allergyTypes.MapGet("/{id}/patients", async (Guid id, [FromServices] IAllergyTypeService service) =>
            Results.Ok(await service.GetPatientsByAllergyTypeIdAsync(id)))
            .WithName("GetAllergyTypePatients")
            .WithSummary("List patients with an allergy type (admin)")
            .WithDescription("Administrators only. Returns every patient who has this allergy recorded.")
            .RequirePermission(Permissions.PatientsReadAll)
            .Produces<List<PatientResponse>>(StatusCodes.Status200OK)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Commands

        allergyTypes.MapPost("", async ([FromBody] AllergyTypeRequest request, [FromServices] IAllergyTypeService service) =>
        {
            var id = await service.AddAllergyTypeAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/allergy-types/{id}", new CreatedResponse(id));
        })
            .WithName("CreateAllergyType")
            .WithSummary("Create an allergy type (admin)")
            .WithDescription("Administrators only. The id in the body is ignored; a new one is generated and returned. name is required.")
            .RequirePermission(Permissions.AllergyTypesManage)
            .ProducesCreated()
            .ProducesValidationProblem()
            .ProducesAuthFailures();

        allergyTypes.MapPut("/{id}", async (Guid id, [FromBody] AllergyTypeRequest request, [FromServices] IAllergyTypeService service) =>
        {
            await service.UpdateAllergyTypeAsync(request with { Id = id });
            return Results.NoContent();
        })
            .WithName("UpdateAllergyType")
            .WithSummary("Update an allergy type (admin)")
            .WithDescription("Administrators only. The id in the route wins over any id in the body. name is required.")
            .RequirePermission(Permissions.AllergyTypesManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        allergyTypes.MapDelete("/{id}", async (Guid id, [FromServices] IAllergyTypeService service) =>
        {
            await service.DeleteAllergyTypeAsync(id);
            return Results.NoContent();
        })
            .WithName("DeleteAllergyType")
            .WithSummary("Delete an allergy type (admin)")
            .WithDescription("Administrators only. Returns 409 while any patient has this allergy recorded.")
            .RequirePermission(Permissions.AllergyTypesManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
