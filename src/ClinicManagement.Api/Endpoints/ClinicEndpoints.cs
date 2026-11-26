using ClinicManagement.Api.Security;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class ClinicEndpoints
{
    public static IEndpointRouteBuilder MapClinicEndpoints(this WebApplication app)
    {
        var clinics = app.MapGroup("/api/clinics").WithTags(ApiTags.Clinics);

        // Every other clinic endpoint needs a signed-in user. The list stays outside this subgroup: the subgroup's
        // sign-in requirement would apply on top of the PublicDoctors toggle.
        var signedIn = clinics.MapGroup("").RequireAuthorization(AuthorizationPolicies.Authenticated);

        // Queries

        // The list carries only clinic contact details and a count, no patient or user data.
        clinics.MapGet("", async ([FromServices] IClinicService service) =>
            Results.Ok(await service.GetAllClinicsAsync()))
            .WithName("GetClinics")
            .WithSummary("List clinics")
            .WithDescription("Every clinic (id, name, address, phoneNumber), each with providerCount: the number of verified providers at the clinic. Public while PublicAccess:Doctors is true (the default), for the doctor directory; otherwise signed-in users only.")
            .RequireAuthorization(AuthorizationPolicies.PublicDoctors)
            .Produces<List<ClinicListItemResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        signedIn.MapGet("/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IClinicService service) =>
            Results.Ok(await service.SearchClinicsAsync(query)))
            .WithName("SearchClinics")
            .WithSummary("Search clinics")
            .WithDescription("Signed-in users. Searches clinic names, addresses, and phone numbers, and returns a page (page from 1, pageSize 1 to 100, default 20). Each item includes providerCount, the number of verified providers at the clinic.")
            .Produces<PagedResponse<ClinicListItemResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        signedIn.MapGet("/{id}", async (Guid id, [FromServices] IClinicService service) =>
            Results.Ok(await service.GetClinicByIdAsync(id)))
            .WithName("GetClinic")
            .WithSummary("Get a clinic")
            .WithDescription("Signed-in users. Returns the clinic's contact details.")
            .Produces<ClinicResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        signedIn.MapGet("/{id}/summary", async (Guid id, [FromServices] IClinicService service) =>
            Results.Ok(await service.GetClinicSummaryAsync(id)))
            .WithName("GetClinicSummary")
            .WithSummary("Get a clinic summary")
            .WithDescription("Signed-in users. Returns the clinic, providerCount (verified providers), todayAppointmentCount (UTC day, declined and cancelled excluded), topSpecialties (up to 3 by verified provider count, ties alphabetical) and up to 5 verified providers ordered by last and first name.")
            .Produces<ClinicSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        signedIn.MapGet("/{id}/providers", async (Guid id, [FromServices] IClinicService service) =>
            Results.Ok(await service.GetProvidersByClinicIdAsync(id)))
            .WithName("GetClinicProviders")
            .WithSummary("List a clinic's providers")
            .WithDescription("Signed-in users. Returns every provider at the clinic, whatever their verification status.")
            .Produces<List<ProviderResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        signedIn.MapGet("/{id}/appointments", async (Guid id, [FromServices] IClinicService service) =>
            Results.Ok(await service.GetAppointmentsByClinicIdAsync(id)))
            .WithName("GetClinicAppointments")
            .WithSummary("List a clinic's appointments")
            .WithDescription("Administrators and administrators of this clinic; other callers get 403. Returns every appointment with a provider at the clinic, unpaged.")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Commands

        signedIn.MapPost("", async ([FromBody] ClinicRequest request, [FromServices] IClinicService service) =>
        {
            var id = await service.AddClinicAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/clinics/{id}", new CreatedResponse(id));
        })
            .WithName("CreateClinic")
            .WithSummary("Create a clinic (admin)")
            .WithDescription("Administrators only. The id in the body is ignored; a new one is generated and returned. name is required.")
            .RequirePermission(Permissions.ClinicsManage)
            .ProducesCreated()
            .ProducesValidationProblem()
            .ProducesAuthFailures();

        signedIn.MapPut("/{id}", async (Guid id, [FromBody] ClinicRequest request, [FromServices] IClinicService service) =>
        {
            await service.UpdateClinicAsync(request with { Id = id });
            return Results.NoContent();
        })
            .WithName("UpdateClinic")
            .WithSummary("Update a clinic (admin)")
            .WithDescription("Administrators only. The id in the route wins over any id in the body. name is required.")
            .RequirePermission(Permissions.ClinicsManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        signedIn.MapDelete("/{id}", async (Guid id, [FromServices] IClinicService service) =>
        {
            await service.DeleteClinicAsync(id);
            return Results.NoContent();
        })
            .WithName("DeleteClinic")
            .WithSummary("Delete a clinic (admin)")
            .WithDescription("Administrators only.")
            .RequirePermission(Permissions.ClinicsManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Clinic administrators

        signedIn.MapPost("/{clinicId}/admins", async (
            Guid clinicId,
            [FromBody] ClinicAdminRequest request,
            [FromServices] IClinicService service) =>
        {
            var id = await service.AddClinicAdminAsync(clinicId, request.IdentitySubject);
            return Results.Created($"/api/clinics/{clinicId}/admins", new CreatedResponse(id));
        })
            .WithName("AddClinicAdmin")
            .WithSummary("Assign a clinic administrator (admin)")
            .WithDescription("Administrators only. Makes the identity subject an administrator of the clinic and returns the new assignment's id. The person does not need to have signed in yet. Returns 409 when they already administer the clinic.")
            .RequirePermission(Permissions.ClinicAdminsAssign)
            .ProducesCreated()
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
