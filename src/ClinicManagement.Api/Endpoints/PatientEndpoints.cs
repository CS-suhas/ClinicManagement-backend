using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Appointments.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManagement.Api.Endpoints;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this WebApplication app)
    {
        app.MapGet("/api/patients", async ([FromServices] IPatientService patientService) =>
        {
            var patients = await patientService.GetAllPatientsAsync();
            return Results.Ok(patients);
        });

        app.MapGet("/api/patients/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientByIdAsync(id)));

        app.MapGet("/api/patients/{id}/allergies", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetPatientAllergiesAsync(id)));

        app.MapGet("/api/patients/{id}/appointments", async (
            Guid id,
            [FromServices] IPatientService patientService) =>
            Results.Ok(await patientService.GetAppointmentsByPatientIdAsync(id)))
            .WithName("GetPatientAppointments")
            .WithSummary("Get appointments for a patient")
            .Produces<List<AppointmentResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/patients", async (
            [FromBody] PatientRequest request,
            [FromServices] IPatientService patientService) =>
        {
            var id = await patientService.AddPatientAsync(request with { Id = Guid.NewGuid() });

            return Results.Created($"/api/patients/{id}", new { id });
        });

        app.MapPut("/api/patients/{id}", async (Guid id, [FromBody] PatientRequest patientRequest, [FromServices] IPatientService patientService) =>
        {
            await patientService.UpdatePatientAsync(patientRequest with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/patients/{id}", async (Guid id, [FromServices] IPatientService patientService) =>
        {
            await patientService.DeletePatientAsync(id);
            return Results.NoContent();
        });
        return app;
    }
}