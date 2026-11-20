using ClinicManagement.Application.Interfaces;

namespace ClinicManagement.Api.Endpoints;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(this WebApplication app)
    {
        app.MapGet("/api/status", async (IStatusService service) =>
                Results.Ok(await service.GetStatusAsync()))
            .WithName("GetStatus")
            .WithSummary("Get API status and resource statistics")
            .WithDescription("Returns API status and totals for clinics, providers, patients, and appointments.")
            .Produces<ClinicManagement.Application.Models.StatusResponse>(StatusCodes.Status200OK);

        app.MapGet("/api/dashboard", async (IStatusService service) =>
                Results.Ok(await service.GetDashboardAsync()))
            .WithName("GetDashboard")
            .WithSummary("Get dashboard counts and upcoming appointments")
            .WithDescription("Returns resource totals and the next five appointments with patient, provider, and clinic names.")
            .Produces<ClinicManagement.Application.Models.DashboardResponse>(StatusCodes.Status200OK);

        return app;
    }
}
