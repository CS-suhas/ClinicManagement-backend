using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Models;
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
        });

        app.MapGet("/api/patients/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.SearchPatientsAsync(query)))
            .WithName("SearchPatients")
            .WithSummary("Search patients")
            .WithDescription("Searches patient names, email, and phone number, and returns a page.")
            .Produces<PagedResponse<PatientResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/patients/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientByIdAsync(id)));

        app.MapGet("/api/patients/{id}/allergies", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientAllergiesAsync(id)));

        app.MapGet("/api/patients/{id}/allergies/search", async (
            Guid id,
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.SearchPatientAllergiesAsync(id, query)))
            .WithName("SearchPatientAllergies")
            .WithSummary("Search a patient's recorded allergies")
            .Produces<PagedResponse<PatientAllergyResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/patients/{id}/allergy-options", async (
            Guid id,
            Guid? selectedAllergyId,
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetSelectableAllergyTypesAsync(id, selectedAllergyId, query)))
            .WithName("GetPatientAllergyOptions")
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
            .WithSummary("Delete a patient's recorded allergy")
            .Produces(StatusCodes.Status204NoContent);

        app.MapGet("/api/patients/{id}/appointments", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetAppointmentsByPatientIdAsync(id)))
            .WithName("GetPatientAppointments")
            .WithSummary("Get appointments for a patient")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/patients", async (
            [FromBody] PatientRequest request,
            [FromServices] IPatientService patientService) =>
        {
            var id = await patientService.AddPatientAsync(request with { Id = Guid.NewGuid() });

            return Results.Created($"/api/patients/{id}", new { id });
        });

        app.MapPut("/api/patients/{id}", async (Guid id, [FromBody] PatientRequest patientRequest, [FromServices] IPatientService patientService) =>
        {
            await patientService.UpdatePatientAsync(patientRequest with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/patients/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
        {
            await patientService.DeletePatientAsync(id);
            return Results.NoContent();
        });
        return app;
    }
}