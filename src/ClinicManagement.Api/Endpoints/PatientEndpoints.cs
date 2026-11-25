using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Models;
using ClinicManagement.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this WebApplication app)
    {
        app.MapGet("/api/patients", async ([FromServices] IPatientService patientService) =>
        {
            var patients = await patientService.GetAllPatientsAsync();
            return Results.Ok(patients);
        })
            .WithSummary("List all patients (admin)")
            .WithDescription("Returns every patient. Each item carries allergyCount (the number of recorded allergies) and highestSeverity (MILD, MODERATE or SEVERE, null when there are none).")
            .RequirePermission(Permissions.PatientsReadAll);

        app.MapGet("/api/patients/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.SearchPatientsAsync(query)))
            .WithName("SearchPatients")
            .RequirePermission(Permissions.PatientsReadAll)
            .WithSummary("Search patients")
            .WithDescription("Searches patient names, email, and phone number, and returns a page. Each item carries allergyCount (the number of recorded allergies) and highestSeverity (MILD, MODERATE or SEVERE, null when there are none).")
            .Produces<PagedResponse<PatientResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/patients/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientByIdAsync(id)))
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        app.MapGet("/api/patients/{id}/summary", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientSummaryAsync(id)))
            .WithName("GetPatientSummary")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get a patient's summary")
            .WithDescription("Returns the patient, their allergies, last and next visit (ignoring declined and cancelled appointments), appointment counts by status, and the three most recent visits. Visible to administrators, the patient, a clinic administrator of a clinic where the patient has an appointment (sees appointments at that clinic only), and a verified doctor with an appointment with the patient (sees their own appointments only). Other callers get 403, including for unknown ids; administrators get 404 for an unknown id.")
            .Produces<PatientSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/patients/{id}/allergies", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientAllergiesAsync(id)))
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        app.MapGet("/api/patients/{id}/allergies/search", async (
            Guid id,
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.SearchPatientAllergiesAsync(id, query)))
            .WithName("SearchPatientAllergies")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Search a patient's recorded allergies")
            .Produces<PagedResponse<PatientAllergyResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/patients/{id}/allergy-options", async (
            Guid id,
            Guid? selectedAllergyId,
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetSelectableAllergyTypesAsync(id, selectedAllergyId, query)))
            .WithName("GetPatientAllergyOptions")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get allergy options not already recorded for a patient")
            .Produces<PagedResponse<AllergyTypeResponse>>(StatusCodes.Status200OK);

        app.MapPost("/api/patients/{id}/allergies", async (
            Guid id,
            [FromBody] PatientAllergyRequest request,
            [FromServices] IPatientService patientService) =>
        {
            var allergyId = await patientService.UpsertPatientAllergyAsync(id, request);
            return Results.Created($"/api/patients/{id}/allergies/{request.AllergyId}", new { allergyId });
        })
            .WithName("AddPatientAllergy")
            .RequirePermission(Permissions.OwnPatientProfileEdit)
            .WithSummary("Add or upsert a patient's allergy")
            .Produces(StatusCodes.Status201Created);

        app.MapPut("/api/patients/{id}/allergies/{allergyId}", async (
            Guid id,
            Guid allergyId,
            [FromBody] PatientAllergyRequest request,
            [FromServices] IPatientService patientService) =>
        {
            if (request.AllergyId != allergyId)
                return Results.BadRequest();
            await patientService.UpsertPatientAllergyAsync(id, request);
            return Results.NoContent();
        })
            .WithName("UpdatePatientAllergy")
            .RequirePermission(Permissions.OwnPatientProfileEdit)
            .WithSummary("Update a patient's recorded allergy")
            .Produces(StatusCodes.Status204NoContent);

        app.MapDelete("/api/patients/{id}/allergies/{allergyId}", async (
            Guid id,
            Guid allergyId,
            [FromServices] IPatientService patientService) =>
        {
            await patientService.DeletePatientAllergyAsync(id, allergyId);
            return Results.NoContent();
        })
            .WithName("DeletePatientAllergy")
            .RequirePermission(Permissions.OwnPatientProfileEdit)
            .WithSummary("Delete a patient's recorded allergy")
            .Produces(StatusCodes.Status204NoContent);

        app.MapGet("/api/patients/{id}/appointments", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetAppointmentsByPatientIdAsync(id)))
            .WithName("GetPatientAppointments")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get appointments for a patient")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/patients", async (
            [FromBody] PatientRequest request,
            [FromServices] IPatientService patientService) =>
        {
            var id = await patientService.AddPatientAsync(request with { Id = Guid.NewGuid() });

            return Results.Created($"/api/patients/{id}", new { id });
        })
            .RequirePermission(Permissions.PatientsManage);

        app.MapPut("/api/patients/{id}", async (Guid id, [FromBody] PatientRequest patientRequest, [FromServices] IPatientService patientService) =>
        {
            await patientService.UpdatePatientAsync(patientRequest with { Id = id });
            return Results.NoContent();
        })
        .RequirePermission(Permissions.OwnPatientProfileEdit);

        app.MapDelete("/api/patients/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
        {
            await patientService.DeletePatientAsync(id);
            return Results.NoContent();
        })
        .RequirePermission(Permissions.PatientsManage);
        return app;
    }
}