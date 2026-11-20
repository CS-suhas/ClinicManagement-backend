using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/appointments", async ([FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetAllAppointmentsAsync()));

        app.MapGet("/api/appointments/{id}", async (Guid id, [FromServices] IAppointmentService service) =>
            Results.Ok(await service.GetAppointmentByIdAsync(id)));

        app.MapPost("/api/appointments", async ([FromBody] AppointmentRequest request, [FromServices] IAppointmentService service) =>
        {
            var id = await service.AddAppointmentAsync(request with { Id = Guid.NewGuid() });
            return Results.Created($"/api/appointments/{id}", new { id });
        });

        app.MapPut("/api/appointments/{id}", async (Guid id, [FromBody] AppointmentRequest request, [FromServices] IAppointmentService service) =>
        {
            await service.UpdateAppointmentAsync(request with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/appointments/{id}", async (Guid id, [FromServices] IAppointmentService service) =>
        {
            await service.DeleteAppointmentAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
