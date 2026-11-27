using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this WebApplication app)
    {
        var referenceData = app.MapGroup("/api").WithTags(ApiTags.ReferenceData);

        referenceData.MapGet("/allergy-severities", () =>
                TypedResults.Ok(Enum.GetValues<AllergySeverity>()))
            .WithName("GetAllergySeverities")
            .WithSummary("List allergy severities")
            .WithDescription("Public. Returns the severity values that can be recorded for a patient's allergy, mildest first.")
            .Produces<AllergySeverity[]>(StatusCodes.Status200OK);

        referenceData.MapGet("/appointment-settings", (ClinicManagementSettings settings) =>
                TypedResults.Ok(new AppointmentSettingsResponse(
                    settings.DefaultAppointmentDurationMinutes,
                    settings.DefaultClinicStartTime,
                    settings.DefaultClinicEndTime)))
            .WithName("GetAppointmentSettings")
            .WithSummary("Get appointment duration and clinic hours")
            .WithDescription("Public. Returns the configured appointment duration in minutes and the clinic opening and closing times, used to build booking slots.")
            .Produces<AppointmentSettingsResponse>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record AppointmentSettingsResponse(
    int AppointmentDurationMinutes,
    TimeOnly ClinicOpeningTime,
    TimeOnly ClinicClosingTime);
