using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
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
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        app.MapGet("/api/providers/search", async (
            [AsParameters] PageQuery query,
            Guid? clinicId,
            ProviderVerificationStatus? verificationStatus,
            [FromServices] IProviderService service) =>
            Results.Ok(await service.SearchProvidersAsync(query, clinicId, verificationStatus)))
            .WithName("SearchProviders")
            .WithSummary("Search all providers (management view)")
            .WithDescription("Searches every provider, including unverified ones, by name, specialty, and clinic, with optional clinic and verification-status filtering. Use the directory endpoint for what patients may see.")
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
            .WithSummary("Self-register as a provider")
            .WithDescription("Creates a provider with no clinic and PENDING verification. They stay out of the directory until an admin verifies them.")
            .Produces(StatusCodes.Status201Created);

        app.MapPost("/api/providers/{id}/verify", async (Guid id, [FromServices] IProviderService service) =>
        {
            await service.VerifyProviderAsync(id);
            return Results.NoContent();
        })
            .WithName("VerifyProvider")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
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
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
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
            .RequireAuthorization(AuthorizationPolicies.DoctorOnly)
            .WithSummary("Get a provider's appointments in a date range")
            .WithDescription("Appointments starting in [from, to), at most 62 days, ordered by time. Declined and cancelled appointments are left out unless includeInactive is true.")
            .Produces<List<AppointmentListResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
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
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

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
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

        return app;
    }
}
