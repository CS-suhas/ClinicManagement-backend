using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using ClinicManagement.Application.Models;
using ClinicManagement.Api.Security;

namespace ClinicManagement.Api.Endpoints;

public static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(this WebApplication app)
    {
        app.MapGet("/api/providers", async ([FromServices] IProviderService service) =>
            Results.Ok(await service.GetAllProvidersAsync()))
            .RequirePermission(Permissions.ProvidersReadAll);

        app.MapGet("/api/providers/search", async (
            [AsParameters] PageQuery query,
            Guid? clinicId,
            ProviderVerificationStatus? verificationStatus,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.SearchProvidersAsync(query, clinicId, verificationStatus)))
            .WithName("SearchProviders")
            .WithSummary("Search providers")
            .WithDescription("Public. Searches providers by name, specialty, and clinic. Anonymous and non-administrator callers only receive verified providers; administrators may also filter by verification status to see unverified ones.")
            .Produces<PagedResponse<ProviderResponse>>(StatusCodes.Status200OK);

        app.MapGet("/api/providers/directory", async (
            [AsParameters] PageQuery query,
            Guid? clinicId,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.SearchProviderDirectoryAsync(query, clinicId)))
            .WithName("SearchProviderDirectory")
            .WithSummary("Search the public provider directory")
            .WithDescription("Only verified providers that belong to a clinic are listed.")
            .Produces<PagedResponse<ProviderResponse>>(StatusCodes.Status200OK);

        app.MapPost("/api/providers/register", async (
            [FromBody] ProviderRegistrationRequest request,
            [FromServices] IProviderService service) =>
        {
            var id = await service.RegisterProviderAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/providers/{id}", new { id });
        })
            .WithName("RegisterProvider")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Self-register as a provider")
            .WithDescription("Creates a provider with no clinic and PENDING verification, linked to the signed-in identity. They stay out of the directory until an admin verifies them. Returns 409 when the identity already has a provider.")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/providers/{id}/verify", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.VerifyProviderAsync(id);
            return Results.NoContent();
        })
            .WithName("VerifyProvider")
            .RequirePermission(Permissions.ProvidersVerify)
            .WithSummary("Verify a provider (admin)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/providers/{id}/reject", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.RejectProviderAsync(id);
            return Results.NoContent();
        })
            .WithName("RejectProvider")
            .RequirePermission(Permissions.ProvidersVerify)
            .WithSummary("Reject a pending provider (admin)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapGet("/api/providers/{id}/calendar", async (
            Guid id,
            DateTimeOffset from,
            DateTimeOffset to,
            bool? includeInactive,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetProviderCalendarAsync(id, from, to, includeInactive ?? false)))
            .WithName("GetProviderCalendar")
            .RequirePermission(Permissions.OwnCalendarRead)
            .WithSummary("Get a provider's appointments in a date range")
            .WithDescription("Appointments starting in [from, to), at most 62 days, ordered by time. Declined and cancelled appointments are left out unless includeInactive is true.")
            .Produces<List<AppointmentListResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/providers/{id}/busy-slots", async (
            Guid id,
            string? date,
            [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetProviderBusySlotsAsync(id, date)))
            .WithName("GetProviderBusySlots")
            .WithSummary("Get a provider's booked start times for a day")
            .WithDescription("Public, with no patient data. date is a UTC calendar date in YYYY-MM-DD form. Returns the start times of the provider's REQUESTED and CONFIRMED appointments on that date, plus the appointment duration. A booking at time T conflicts when an active appointment starts strictly after T minus the duration and strictly before T plus the duration. Returns 404 for an unknown provider, or one that is not verified or has no clinic.")
            .Produces<ProviderBusySlotsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/providers/{id}/summary", async (
            Guid id,
            [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetProviderSummaryAsync(id)))
            .WithName("GetProviderSummary")
            .WithSummary("Get a bookable provider's summary and next free slot")
            .WithDescription("Public, with no patient data. Returns the provider, its clinic, nextAvailableStart and todayAppointmentCount. nextAvailableStart is the earliest slot at or after now within the next 14 UTC days, or null if none is free. Slots start at clinic opening plus multiples of the appointment duration and must end by closing (both inclusive). A slot conflicts with a REQUESTED or CONFIRMED appointment that starts strictly within one duration of it. todayAppointmentCount covers the UTC calendar day and excludes declined and cancelled appointments. Returns 404 for an unknown provider, or one that is not verified or has no clinic.")
            .Produces<ProviderSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/providers/{id}/patients", async (
            Guid id,
            [AsParameters] PageQuery query,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetPatientRosterAsync(id, query)))
            .WithName("GetProviderPatientRoster")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("List a provider's patients")
            .WithDescription("Patients with at least one appointment with the provider, paged and optionally searched by patient name (search). Each row has the latest past and earliest upcoming visit relative to now, ignoring declined and cancelled appointments. Visible to the provider, an administrator, or a clinic administrator of the provider's clinic.")
            .Produces<PagedResponse<ProviderPatientRosterItem>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/providers/{id}", async (Guid id, [FromServices] IProviderService service) =>
            Results.Ok(await service.GetProviderByIdAsync(id)));

        app.MapGet("/api/providers/{id}/clinic", async (
            Guid id,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetClinicByProviderIdAsync(id)))
            .WithName("GetProviderClinic")
            .WithSummary("Get a provider's clinic")
            .Produces<ClinicResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/providers/{id}/appointments", async (
            Guid id,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetAppointmentsByProviderIdAsync(id)))
            .WithName("GetProviderAppointments")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .WithSummary("Get appointments for a provider")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/providers", async ([FromBody] ProviderRequest request, [FromServices] IProviderService service) =>
        {
            var id = await service.AddProviderAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/providers/{id}", new { id });
        })
        .RequirePermission(Permissions.ProvidersManage);

        app.MapPut("/api/providers/{id}", async (Guid id, [FromBody] ProviderRequest request, [FromServices] IProviderService service) =>
        {
            await service.UpdateProviderAsync(request with { Id = id });
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.Authenticated);

        app.MapDelete("/api/providers/{id}", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.DeleteProviderAsync(id);
            return Results.NoContent();
        })
        .RequirePermission(Permissions.ProvidersManage);

        return app;
    }
}
