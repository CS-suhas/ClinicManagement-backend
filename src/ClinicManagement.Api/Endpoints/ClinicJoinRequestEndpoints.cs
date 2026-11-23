using ClinicManagement.Application.ClinicJoinRequests.Interfaces;
using ClinicManagement.Application.ClinicJoinRequests.Models;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Api.Security;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class ClinicJoinRequestEndpoints
{
    public static IEndpointRouteBuilder MapClinicJoinRequestEndpoints(this WebApplication app)
    {
        app.MapPost("/api/providers/{providerId}/clinic-join-requests", async (
            Guid providerId,
            [FromBody] ClinicJoinRequestRequest request,
            [FromServices] IClinicJoinRequestService service) =>
        {
            var id = await service.RequestToJoinAsync(providerId, request.ClinicId);
            return Results.Created($"/api/clinic-join-requests/{id}", new { id });
        })
            .WithName("RequestToJoinClinic")
            .RequireAuthorization(AuthorizationPolicies.DoctorOnly)
            .WithSummary("Ask a clinic to accept a provider")
            .WithDescription("The provider must be verified and not already in a clinic, and can have only one pending request.")
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapGet("/api/providers/{providerId}/clinic-join-requests", async (
            Guid providerId,
            [FromServices] IClinicJoinRequestService service) =>
            Results.Ok(await service.GetByProviderIdAsync(providerId)))
            .WithName("GetProviderClinicJoinRequests")
            .WithSummary("Get a provider's join requests, newest first")
            .Produces<IReadOnlyList<ClinicJoinRequestResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/clinics/{clinicId}/join-requests", async (
            Guid clinicId,
            ClinicJoinRequestStatus? status,
            [FromServices] IClinicJoinRequestService service) =>
            Results.Ok(await service.GetByClinicIdAsync(clinicId, status)))
            .WithName("GetClinicJoinRequests")
            .WithSummary("Get the join requests a clinic must decide on")
            .WithDescription("Optionally filtered by status, for example PENDING.")
            .Produces<IReadOnlyList<ClinicJoinRequestResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/clinic-join-requests/{id}/approve", async (
            Guid id,
            [FromServices] IClinicJoinRequestService service) =>
        {
            await service.ApproveAsync(id);
            return Results.NoContent();
        })
            .WithName("ApproveClinicJoinRequest")
            .RequireAuthorization(AuthorizationPolicies.ClinicAdminOnly)
            .WithSummary("Approve a join request and assign the provider to the clinic")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/clinic-join-requests/{id}/reject", async (
            Guid id,
            [FromServices] IClinicJoinRequestService service) =>
        {
            await service.RejectAsync(id);
            return Results.NoContent();
        })
            .WithName("RejectClinicJoinRequest")
            .RequireAuthorization(AuthorizationPolicies.ClinicAdminOnly)
            .WithSummary("Reject a pending join request")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/clinic-join-requests/{id}/cancel", async (
            Guid id,
            [FromServices] IClinicJoinRequestService service) =>
        {
            await service.CancelAsync(id);
            return Results.NoContent();
        })
            .WithName("CancelClinicJoinRequest")
            .WithSummary("Withdraw a pending join request (by the provider)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }
}
