using ClinicManagement.Api.Security;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class ProviderEndpoints
{
    private const string AppointmentViewerRule =
        "Visible to administrators, the provider themself, and administrators of the provider's clinic; other callers get 403.";

    public static IEndpointRouteBuilder MapProviderEndpoints(this WebApplication app)
    {
        var providers = app.MapGroup("/api/providers").WithTags(ApiTags.Providers);

        providers.MapGet("/search", async (
            [AsParameters] PageQuery query,
            Guid? clinicId,
            ProviderVerificationStatus? verificationStatus,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.SearchProvidersAsync(query, clinicId, verificationStatus)))
            .WithName("SearchProviders")
            .WithSummary("Search providers")
            .WithDescription("Public. Searches providers by name, specialty, and clinic, optionally within clinicId, and returns a page (page from 1, pageSize 1 to 100, default 20). Anonymous and non-administrator callers only receive verified providers; administrators may also filter by verificationStatus to see unverified ones.")
            .Produces<PagedResponse<ProviderResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        providers.MapGet("/directory", async (
            [AsParameters] PageQuery query,
            Guid? clinicId,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.SearchProviderDirectoryAsync(query, clinicId)))
            .WithName("SearchProviderDirectory")
            .WithSummary("Search the public provider directory")
            .WithDescription("Public. Lists verified providers only, optionally searched and limited to clinicId, as a page (page from 1, pageSize 1 to 100, default 20).")
            .Produces<PagedResponse<ProviderResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        providers.MapGet("/{id}", async (Guid id, [FromServices] IProviderService service) =>
            Results.Ok(await service.GetProviderByIdAsync(id)))
            .WithName("GetProvider")
            .WithSummary("Get a provider")
            .WithDescription("Public. Returns the provider's profile, including its verification status.")
            .Produces<ProviderResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapGet("/{id}/clinic", async (Guid id, [FromServices] IProviderService service) =>
            Results.Ok(await service.GetClinicByProviderIdAsync(id)))
            .WithName("GetProviderClinic")
            .WithSummary("Get a provider's clinic")
            .WithDescription("Public. Returns 404 for an unknown provider, or one without a clinic.")
            .Produces<ClinicResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapGet("/{id}/summary", async (Guid id, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetProviderSummaryAsync(id)))
            .WithName("GetProviderSummary")
            .WithSummary("Get a bookable provider's summary and next free slot")
            .WithDescription("Public, with no patient data. Returns the provider, its clinic, nextAvailableStart and todayAppointmentCount. nextAvailableStart is the earliest slot at or after now within the next 14 UTC days, or null if none is free. Slots start at clinic opening plus multiples of the appointment duration and must end by closing (both inclusive). A slot conflicts with a REQUESTED or CONFIRMED appointment that starts strictly within one duration of it. todayAppointmentCount covers the UTC calendar day and excludes declined and cancelled appointments. Returns 404 for an unknown provider, or one that is not verified.")
            .Produces<ProviderSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapGet("/{id}/busy-slots", async (Guid id, string? date, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetProviderBusySlotsAsync(id, date)))
            .WithName("GetProviderBusySlots")
            .WithSummary("Get a provider's booked start times for a day")
            .WithDescription("Public, with no patient data. date is a UTC calendar date in YYYY-MM-DD form. Returns the start times of the provider's REQUESTED and CONFIRMED appointments on that date, plus the appointment duration. A booking at time T conflicts when an active appointment starts strictly after T minus the duration and strictly before T plus the duration. Returns 404 for an unknown provider, or one that is not verified.")
            .Produces<ProviderBusySlotsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapGet("", async ([FromServices] IProviderService service) =>
            Results.Ok(await service.GetAllProvidersAsync()))
            .WithName("GetProviders")
            .WithSummary("List all providers (admin)")
            .WithDescription("Administrators only. Returns every provider, whatever their verification status, unpaged.")
            .RequirePermission(Permissions.ProvidersReadAll)
            .Produces<List<ProviderResponse>>(StatusCodes.Status200OK)
            .ProducesAuthFailures();

        providers.MapGet("/{id}/calendar", async (
            Guid id,
            DateTimeOffset from,
            DateTimeOffset to,
            bool? includeInactive,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetProviderCalendarAsync(id, from, to, includeInactive ?? false)))
            .WithName("GetProviderCalendar")
            .WithSummary("Get a provider's appointments in a date range (provider)")
            .WithDescription("Only the provider themself, once verified. Appointments starting in [from, to), at most 62 days, ordered by time. Declined and cancelled appointments are left out unless includeInactive is true.")
            .RequirePermission(Permissions.OwnCalendarRead)
            .Produces<List<AppointmentListResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesAuthFailures();

        providers.MapGet("/{id}/appointments", async (Guid id, [FromServices] IProviderService service) =>
            Results.Ok(await service.GetAppointmentsByProviderIdAsync(id)))
            .WithName("GetProviderAppointments")
            .WithSummary("List a provider's appointments")
            .WithDescription("Returns every appointment of the provider, unpaged. " + AppointmentViewerRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapGet("/{id}/patients", async (
            Guid id,
            [AsParameters] PageQuery query,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.GetPatientRosterAsync(id, query)))
            .WithName("GetProviderPatientRoster")
            .WithSummary("List a provider's patients")
            .WithDescription("Patients with at least one appointment with the provider, paged and optionally searched by patient name (search). Each row has the latest past and earliest upcoming visit relative to now, ignoring declined and cancelled appointments. " + AppointmentViewerRule)
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces<PagedResponse<ProviderPatientRosterItem>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapPost("/register", async (
            [FromBody] ProviderRegistrationRequest request,
            [FromServices] IProviderService service) =>
        {
            var id = await service.RegisterProviderAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/providers/{id}", new CreatedResponse(id));
        })
            .WithName("RegisterProvider")
            .WithSummary("Self-register as a provider")
            .WithDescription("Signed-in users. Creates a provider at the given clinic with PENDING verification, linked to the signed-in identity. They stay out of the directory until an admin verifies them. firstName, lastName, specialty and clinicId are required. Returns 404 when the clinic does not exist and 409 when the identity already has a provider.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .ProducesCreated()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        providers.MapPost("/{id}/verify", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.VerifyProviderAsync(id);
            return Results.NoContent();
        })
            .WithName("VerifyProvider")
            .WithSummary("Verify a provider")
            .WithDescription("Administrators, and administrators of the provider's clinic; other callers get 403. Moves a PENDING or REJECTED provider to VERIFIED, which lists them in the directory and lets patients book them. Returns 409 when the provider is already verified or changed concurrently.")
            .RequirePermission(Permissions.ProvidersVerify)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        providers.MapPost("/{id}/reject", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.RejectProviderAsync(id);
            return Results.NoContent();
        })
            .WithName("RejectProvider")
            .WithSummary("Reject a pending provider")
            .WithDescription("Administrators, and administrators of the provider's clinic; other callers get 403. Moves a PENDING provider to REJECTED. Returns 409 from any other status, or when the provider changed concurrently.")
            .RequirePermission(Permissions.ProvidersVerify)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        providers.MapPost("", async ([FromBody] ProviderRequest request, [FromServices] IProviderService service) =>
        {
            var id = await service.AddProviderAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/providers/{id}", new CreatedResponse(id));
        })
            .WithName("CreateProvider")
            .WithSummary("Create a provider (admin)")
            .WithDescription("Administrators only. Creates a provider not linked to any identity; doctors sign up with POST /api/providers/register. The id in the body is ignored. firstName, lastName, specialty and clinicId are required. Returns 404 when the clinic does not exist.")
            .RequirePermission(Permissions.ProvidersManage)
            .ProducesCreated()
            .ProducesValidationProblem()
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapPut("/{id}", async (Guid id, [FromBody] ProviderRequest request, [FromServices] IProviderService service) =>
        {
            await service.UpdateProviderAsync(request with { Id = id });
            return Results.NoContent();
        })
            .WithName("UpdateProvider")
            .WithSummary("Update a provider")
            .WithDescription("Administrators, or the provider themself; other callers get 403. Providers cannot move themselves to another clinic (403). The id in the route wins over any id in the body. Returns 404 for an unknown provider or clinic.")
            .RequireAuthorization(AuthorizationPolicies.Authenticated)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        providers.MapDelete("/{id}", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.DeleteProviderAsync(id);
            return Results.NoContent();
        })
            .WithName("DeleteProvider")
            .WithSummary("Delete a provider (admin)")
            .WithDescription("Administrators only. Returns 409 while the provider has any appointments.")
            .RequirePermission(Permissions.ProvidersManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesAuthFailures()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
