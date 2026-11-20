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

        return app;
    }
}
