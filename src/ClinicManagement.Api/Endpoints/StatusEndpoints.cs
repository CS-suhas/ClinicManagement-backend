using ClinicManagement.Api.Security;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Api.Endpoints;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(this WebApplication app)
    {
        var status = app.MapGroup("/api").WithTags(ApiTags.Status);

        status.MapGet("/status", async (IStatusService service) =>
                Results.Ok(await service.GetStatusAsync()))
            .WithName("GetStatus")
            .WithSummary("Get API status and resource totals")
            .WithDescription("Public. Returns status \"ok\" and the totals of clinics, providers, patients, and appointments. For a plain liveness probe use GET /health.")
            .Produces<StatusResponse>(StatusCodes.Status200OK);

        status.MapGet("/dashboard", async (IStatusService service) =>
                Results.Ok(await service.GetDashboardAsync()))
            .WithName("GetDashboard")
            .WithSummary("Get the platform dashboard (admin)")
            .WithDescription("Administrators only. Returns resource totals and the next five appointments with patient, provider, and clinic names. Signed-in users get their own role-aware dashboard from GET /api/me/dashboard.")
            .RequirePermission(Permissions.PlatformDashboardRead)
            .Produces<DashboardResponse>(StatusCodes.Status200OK)
            .ProducesAuthFailures();

        return app;
    }
}
