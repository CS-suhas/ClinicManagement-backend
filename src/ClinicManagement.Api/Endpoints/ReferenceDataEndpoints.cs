using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this WebApplication app)
    {
        app.MapGet("/api/allergy-severities", () =>
                TypedResults.Ok(Enum.GetNames<AllergySeverity>()))
            .WithName("GetAllergySeverities")
            .WithSummary("Get available allergy severity levels")
            .WithDescription("Returns the severity values that can be assigned to a patient's allergy.")
            .Produces<string[]>(StatusCodes.Status200OK);

        app.MapGet("/api/appointment-settings", (ClinicManagementSettings settings) =>
                TypedResults.Ok(new AppointmentSettingsResponse(
                    settings.DefaultAppointmentDurationMinutes,
                    settings.DefaultClinicStartTime,
                    settings.DefaultClinicEndTime)))
            .WithName("GetAppointmentSettings")
            .WithSummary("Get appointment duration and clinic hours")
            .WithDescription("Returns the configured appointment duration and clinic opening hours.")
            .Produces<AppointmentSettingsResponse>(StatusCodes.Status200OK);

        return app;
    }
}

/// <summary>Appointment duration and opening hours used by the clinic.</summary>
public sealed record AppointmentSettingsResponse(
    int AppointmentDurationMinutes,
    TimeOnly ClinicOpeningTime,
    TimeOnly ClinicClosingTime);
