using ClinicManagement.Api.Security;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Providers.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class AppointmentEndpoints
{
    private const string ViewerRule =
        "Visible to administrators, the appointment's patient and provider, and administrators of the provider's clinic; other callers get 403.";

    public static IEndpointRouteBuilder MapAppointmentEndpoints(this WebApplication app)
    {
        var appointments = app.MapGroup("/api/appointments").WithTags(ApiTags.Appointments);

        // Queries

        appointments.MapGet("", async ([FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetAllAppointmentsAsync()))
            .WithName("GetAppointments")
            .WithSummary("List all appointments (admin)")
            .WithDescription("Administrators only. Returns every appointment, unpaged. Use GET /api/appointments/search for paged, filtered lists.")
            .RequirePermission(Permissions.AppointmentsReadAll)
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .ProducesAuthFailures();

        appointments.MapGet("/search", async (
            [AsParameters] AppointmentSearchQuery query,
            [FromServices] IAppointmentService service) =>
            Results.Ok(await service.SearchAppointmentsAsync(query)))
            .WithName("SearchAppointments")
            .WithSummary("Search and filter appointments")
            .WithDescription("Filters appointments by text, date range (from < to when both are given), clinic, provider, providerIds (repeatable, up to 100), status, or patient, and returns a page (page from 1, pageSize 1 to 100, default 20). Patients see only their own appointments and providers only their own; clinic administrators are limited to their clinic. Each item includes the patient's allergy summary (patientAllergyCount and patientHighestAllergySeverity, null when none) and timestamps: createdAtUtc is when the appointment was booked, and statusChangedAtUtc is when its status last changed (equal to createdAtUtc if it never has).")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PagedResponse<AppointmentListResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        appointments.MapGet("/{id}", async (Guid id, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetAppointmentByIdAsync(id)))
            .WithName("GetAppointment")
            .WithSummary("Get an appointment")
            .WithDescription(ViewerRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<AppointmentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        appointments.MapGet("/{id}/patient", async (Guid id, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetPatientByAppointmentIdAsync(id)))
            .WithName("GetAppointmentPatient")
            .WithSummary("Get the patient of an appointment")
            .WithDescription(ViewerRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PatientResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        appointments.MapGet("/{id}/provider", async (Guid id, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetProviderByAppointmentIdAsync(id)))
            .WithName("GetAppointmentProvider")
            .WithSummary("Get the provider of an appointment")
            .WithDescription(ViewerRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<ProviderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        appointments.MapGet("/{id}/clinic", async (Guid id, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetClinicByAppointmentIdAsync(id)))
            .WithName("GetAppointmentClinic")
            .WithSummary("Get the clinic of an appointment")
            .WithDescription("Returns the clinic of the appointment's provider. " + ViewerRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<ClinicResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Commands

        appointments.MapPost("", async ([FromBody] AppointmentRequest request, [FromServices] IAppointmentService service) =>
        {
            var id = await service.AddAppointmentAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/appointments/{id}", new CreatedResponse(id));
        })
            .WithName("BookAppointment")
            .WithSummary("Book an appointment (patient)")
            .WithDescription("Patients only. Books a REQUESTED appointment for the signed-in patient; id and patientId in the body are ignored. appointmentDateTime must be in the future, and the appointment must start at or after clinic opening and end by closing. Returns 404 for an unknown provider, and 409 when the provider is not verified or already has an appointment within one appointment duration of the requested time.")
            .RequirePermission(Permissions.AppointmentsBook)
            .ProducesCreated()
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        appointments.MapPut("/{id}", async (Guid id, [FromBody] AppointmentRequest request, [FromServices] IAppointmentService service) =>
        {
            await service.UpdateAppointmentAsync(request with { Id = id });
            return Results.NoContent();
        })
            .WithName("UpdateAppointment")
            .WithSummary("Update an appointment (admin)")
            .WithDescription("Administrators only. Replaces the patient, provider, time and reason. The new time follows the same rules as booking, and moving the appointment to another time or provider returns 409 when it would overlap another appointment.")
            .RequirePermission(Permissions.AppointmentsManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        appointments.MapDelete("/{id}", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.DeleteAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("DeleteAppointment")
            .WithSummary("Delete an appointment (admin)")
            .WithDescription("Administrators only. Removes the appointment permanently. To end an appointment while keeping its history, cancel it instead.")
            .RequirePermission(Permissions.AppointmentsManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Status transitions

        appointments.MapPost("/{id}/confirm", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.ConfirmAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("ConfirmAppointment")
            .WithSummary("Confirm a requested appointment (provider)")
            .WithDescription("Only the appointment's own verified provider. Moves REQUESTED to CONFIRMED. Returns 409 from any other status, or when the appointment changed concurrently.")
            .RequirePermission(Permissions.AppointmentsRespond)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        appointments.MapPost("/{id}/decline", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.DeclineAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("DeclineAppointment")
            .WithSummary("Decline a requested appointment (provider)")
            .WithDescription("Only the appointment's own verified provider. Moves REQUESTED to DECLINED. Returns 409 from any other status, or when the appointment changed concurrently.")
            .RequirePermission(Permissions.AppointmentsRespond)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        appointments.MapPost("/{id}/cancel", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.CancelAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("CancelAppointment")
            .WithSummary("Cancel an appointment (patient or provider)")
            .WithDescription("Only the appointment's patient or provider; anyone else gets 403. Moves REQUESTED or CONFIRMED to CANCELLED. Returns 409 from any other status, or when the appointment changed concurrently.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        appointments.MapPost("/{id}/complete", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.CompleteAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("CompleteAppointment")
            .WithSummary("Complete a confirmed appointment (provider)")
            .WithDescription("Only the appointment's own verified provider. Moves CONFIRMED to COMPLETED. Returns 409 from any other status, or when the appointment changed concurrently.")
            .RequirePermission(Permissions.AppointmentsRespond)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
