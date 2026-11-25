using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Models;
using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Api.Security;

namespace ClinicManagement.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/appointments", async ([FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetAllAppointmentsAsync()))
            .RequirePermission(Permissions.AppointmentsReadAll);

        app.MapGet("/api/appointments/search", async (
            [AsParameters] AppointmentSearchQuery query,
            [FromServices] IAppointmentService service) =>
            Results.Ok(await service.SearchAppointmentsAsync(query)))
            .WithName("SearchAppointments")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Search and filter appointments")
            .WithDescription("Filters appointments by text, date range (from < to when both are given), clinic, provider, providerIds (repeatable, up to 100), or patient, and returns a page. Patients see only their own appointments and providers only their own; clinic administrators are limited to their clinic. Each item includes the patient's allergy summary (patientAllergyCount and patientHighestAllergySeverity, null when none) and timestamps: createdAtUtc is when the appointment was booked, and statusChangedAtUtc is when its status last changed (equal to createdAtUtc if it never has).")
            .Produces<PagedResponse<AppointmentListResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden);

        app.MapGet("/api/appointments/{id}", async (Guid id, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetAppointmentByIdAsync(id)))
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        app.MapGet("/api/appointments/{id}/patient", async (
            Guid id,
            [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetPatientByAppointmentIdAsync(id)))
            .WithName("GetAppointmentPatient")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get the patient linked to an appointment")
            .Produces<PatientResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/appointments/{id}/provider", async (
            Guid id,
            [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetProviderByAppointmentIdAsync(id)))
            .WithName("GetAppointmentProvider")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get the provider linked to an appointment")
            .Produces<ProviderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/appointments/{id}/clinic", async (
            Guid id,
            [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetClinicByAppointmentIdAsync(id)))
            .WithName("GetAppointmentClinic")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get the clinic for an appointment's provider")
            .Produces<ClinicResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/appointments", async ([FromBody] AppointmentRequest request, [FromServices] IAppointmentService service) =>
        {
            var id = await service.AddAppointmentAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/appointments/{id}", new { id });
        })
        .RequirePermission(Permissions.AppointmentsBook);

        app.MapPut("/api/appointments/{id}", async (Guid id, [FromBody] AppointmentRequest request, [FromServices] IAppointmentService service) =>
        {
            await service.UpdateAppointmentAsync(request with { Id = id });
            return Results.NoContent();
        })
        .RequirePermission(Permissions.AppointmentsManage);

        app.MapDelete("/api/appointments/{id}", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.DeleteAppointmentAsync(id);
            return Results.NoContent();
        })
        .RequirePermission(Permissions.AppointmentsManage);

        app.MapPost("/api/appointments/{id}/confirm", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.ConfirmAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("ConfirmAppointment")
            .RequirePermission(Permissions.AppointmentsRespond)
            .WithSummary("Accept a requested appointment (provider)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/appointments/{id}/decline", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.DeclineAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("DeclineAppointment")
            .RequirePermission(Permissions.AppointmentsRespond)
            .WithSummary("Decline a requested appointment (provider)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/appointments/{id}/cancel", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.CancelAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("CancelAppointment")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Cancel a requested or confirmed appointment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/appointments/{id}/complete", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.CompleteAppointmentAsync(id);
            return Results.NoContent();
        })
            .WithName("CompleteAppointment")
            .RequirePermission(Permissions.AppointmentsRespond)
            .WithSummary("Mark a confirmed appointment as completed")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
