using ClinicManagement.Api.Security;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class PatientEndpoints
{
    private const string SelfOrAdminRule = "Visible to administrators and the patient themself; other callers get 403.";
    private const string SelfOnlyRule = "Only the patient themself; other callers, administrators included, get 403.";

    public static IEndpointRouteBuilder MapPatientEndpoints(this WebApplication app)
    {
        var patients = app.MapGroup("/api/patients").WithTags(ApiTags.Patients);

        // Queries

        patients.MapGet("", async ([FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetAllPatientsAsync()))
            .WithName("GetPatients")
            .WithSummary("List all patients (admin)")
            .WithDescription("Administrators only. Returns every patient, unpaged. Each item carries allergyCount (the number of recorded allergies) and highestSeverity (MILD, MODERATE or SEVERE, null when there are none).")
            .RequirePermission(Permissions.PatientsReadAll)
            .Produces<List<PatientListItemResponse>>(StatusCodes.Status200OK)
            .ProducesAuthFailures();

        patients.MapGet("/search", async (
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.SearchPatientsAsync(query)))
            .WithName("SearchPatients")
            .WithSummary("Search patients (admin)")
            .WithDescription("Administrators only. Searches patient names, email, and phone number, and returns a page (page from 1, pageSize 1 to 100, default 20). Each item carries allergyCount (the number of recorded allergies) and highestSeverity (MILD, MODERATE or SEVERE, null when there are none).")
            .RequirePermission(Permissions.PatientsReadAll)
            .Produces<PagedResponse<PatientListItemResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesAuthFailures();

        patients.MapGet("/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientByIdAsync(id)))
            .WithName("GetPatient")
            .WithSummary("Get a patient")
            .WithDescription(SelfOrAdminRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PatientResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapGet("/{id}/summary", async (Guid id, [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientSummaryAsync(id)))
            .WithName("GetPatientSummary")
            .WithSummary("Get a patient's summary")
            .WithDescription("Returns the patient, their allergies, last and next visit (ignoring declined and cancelled appointments), appointment counts by status, and the three most recent visits. Visible to administrators, the patient, a clinic administrator of a clinic where the patient has an appointment (sees appointments at that clinic only), and a verified doctor with an appointment with the patient (sees their own appointments only). Other callers get 403, including for unknown ids; administrators get 404 for an unknown id.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PatientSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapGet("/{id}/appointments", async (Guid id, [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetAppointmentsByPatientIdAsync(id)))
            .WithName("GetPatientAppointments")
            .WithSummary("List a patient's appointments")
            .WithDescription("Returns every appointment of the patient, unpaged. " + SelfOrAdminRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Commands

        patients.MapPost("", async ([FromBody] PatientRequest request, [FromServices] IPatientService patientService) =>
        {
            var id = await patientService.AddPatientAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/patients/{id}", new CreatedResponse(id));
        })
            .WithName("CreatePatient")
            .WithSummary("Create a patient (admin)")
            .WithDescription("Administrators only. Creates a patient not linked to any identity; signed-in users create their own with POST /api/me/patient. The id in the body is ignored. firstName, lastName, dateOfBirth (not in the future), phoneNumber and a valid email are required.")
            .RequirePermission(Permissions.PatientsManage)
            .ProducesCreated()
            .ProducesValidationProblem()
            .ProducesAuthFailures();

        patients.MapPut("/{id}", async (Guid id, [FromBody] PatientRequest request, [FromServices] IPatientService patientService) =>
        {
            await patientService.UpdatePatientAsync(request with { Id = id });
            return Results.NoContent();
        })
            .WithName("UpdatePatient")
            .WithSummary("Update a patient's profile (patient)")
            .WithDescription(SelfOnlyRule + " The id in the route wins over any id in the body. Fields are validated as on create.")
            .RequirePermission(Permissions.OwnPatientProfileEdit)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapDelete("/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
        {
            await patientService.DeletePatientAsync(id);
            return Results.NoContent();
        })
            .WithName("DeletePatient")
            .WithSummary("Delete a patient (admin)")
            .WithDescription("Administrators only. Returns 409 while the patient has recorded allergies or appointments.")
            .RequirePermission(Permissions.PatientsManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Allergies

        patients.MapGet("/{id}/allergies", async (Guid id, [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientAllergiesAsync(id)))
            .WithName("GetPatientAllergies")
            .WithSummary("List a patient's allergies")
            .WithDescription("Returns every allergy recorded for the patient, with severity and notes. " + SelfOrAdminRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<List<PatientAllergyResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapGet("/{id}/allergies/search", async (
            Guid id,
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.SearchPatientAllergiesAsync(id, query)))
            .WithName("SearchPatientAllergies")
            .WithSummary("Search a patient's allergies")
            .WithDescription("Searches the patient's recorded allergies and returns a page (page from 1, pageSize 1 to 100, default 20). " + SelfOrAdminRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PagedResponse<PatientAllergyResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapGet("/{id}/allergy-options", async (
            Guid id,
            Guid? selectedAllergyId,
            [AsParameters] PageQuery query,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetSelectableAllergyTypesAsync(id, selectedAllergyId, query)))
            .WithName("GetPatientAllergyOptions")
            .WithSummary("List allergy types a patient can still record")
            .WithDescription("Returns a page of allergy types not yet recorded for the patient, for an allergy picker. Pass selectedAllergyId when editing an existing allergy so that it stays in the options. " + SelfOrAdminRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PagedResponse<AllergyTypeResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapPost("/{id}/allergies", async (
            Guid id,
            [FromBody] PatientAllergyRequest request,
            [FromServices] IPatientService patientService) =>
        {
            var allergyId = await patientService.UpsertPatientAllergyAsync(id, request);
            return Results.Created($"/api/patients/{id}/allergies/{request.AllergyId}", new { allergyId });
        })
            .WithName("AddPatientAllergy")
            .WithSummary("Record an allergy (patient)")
            .WithDescription(SelfOnlyRule + " Records allergyId with a severity (see GET /api/allergy-severities) and optional notes; if it is already recorded, its severity and notes are replaced. The body of the 201 is { allergyId }. Returns 404 for an unknown patient or allergy type.")
            .RequirePermission(Permissions.OwnPatientProfileEdit)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapPut("/{id}/allergies/{allergyId}", async (
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
            .WithSummary("Update a recorded allergy (patient)")
            .WithDescription(SelfOnlyRule + " Replaces the severity and notes. allergyId in the body must match the route, otherwise 400.")
            .RequirePermission(Permissions.OwnPatientProfileEdit)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        patients.MapDelete("/{id}/allergies/{allergyId}", async (
            Guid id,
            Guid allergyId,
            [FromServices] IPatientService patientService) =>
        {
            await patientService.DeletePatientAllergyAsync(id, allergyId);
            return Results.NoContent();
        })
            .WithName("DeletePatientAllergy")
            .WithSummary("Remove a recorded allergy (patient)")
            .WithDescription(SelfOnlyRule + " Returns 404 when the allergy is not recorded for the patient.")
            .RequirePermission(Permissions.OwnPatientProfileEdit)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
