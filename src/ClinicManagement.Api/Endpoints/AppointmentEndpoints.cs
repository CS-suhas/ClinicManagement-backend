using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/appointments", async ([FromServices] IAppointmentManagementService service) =>
            Results.Ok(await service.GetAllAppointmentsAsync()));

        app.MapGet("/api/appointments/{id}", async (Guid id, [FromServices] IAppointmentManagementService service) =>
        {
            var appointment = await service.GetAppointmentByIdAsync(id);
            return appointment is null ? Results.NotFound() : Results.Ok(appointment);
        });

        app.MapPost("/api/appointments", async ([FromBody] AppointmentRequest request, [FromServices] IAppointmentManagementService service) =>
        {
            try
            {
                var id = await service.AddAppointmentAsync(request with { Id = Guid.NewGuid() });
                return Results.Created($"/api/appointments/{id}", new { id });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.BadRequest(exception.Message);
            }
            catch (ProviderAppointmentOverlapException exception)
            {
                return Results.Conflict(exception.Message);
            }
        });

        app.MapPut("/api/appointments/{id}", async (Guid id, [FromBody] AppointmentRequest request, [FromServices] IAppointmentManagementService service) =>
        {
            if (await service.GetAppointmentByIdAsync(id) is null)
                return Results.NotFound();

            try
            {
                await service.UpdateAppointmentAsync(request with { Id = id });
                return Results.NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return Results.BadRequest(exception.Message);
            }
            catch (ProviderAppointmentOverlapException exception)
            {
                return Results.Conflict(exception.Message);
            }
        });

        app.MapDelete("/api/appointments/{id}", async (Guid id, [FromServices] IAppointmentManagementService service) =>
        {
            if (await service.GetAppointmentByIdAsync(id) is null)
                return Results.NotFound();

            await service.DeleteAppointmentAsync(id);
            return Results.NoContent();
        });

        return app;
    }
}
