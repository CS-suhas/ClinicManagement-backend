using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;

namespace ClinicManagement.Api.Endpoints;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this WebApplication app)
    {
        app.MapGet("/api/patients", async (IPatientManagementService patientService) =>
        {
            var patients = await patientService.GetAllPatientsAsync();
            return Results.Ok(patients);
        });

        app.MapGet("/api/patients/{id}", async (Guid id, IPatientManagementService patientService) =>
        {
            var patient = await patientService.GetPatientByIdAsync(id);
            if (patient == null)
            {
                return Results.NotFound();
            }
            return Results.Ok(patient);
        });

        app.MapPost("/api/patients", async (
            PatientRequest request,
            IPatientManagementService patientService) =>
        {
            var id = await patientService.AddPatientAsync(request with { Id = Guid.NewGuid() });

            return Results.Created($"/api/patients/{id}", new { id });
        });

        app.MapPut("/api/patients/{id}", async (Guid id, PatientRequest patientRequest, IPatientManagementService patientService) =>
        {
            var existingPatient = await patientService.GetPatientByIdAsync(id);
            if (existingPatient == null)
            {
                return Results.NotFound();
            }

            await patientService.UpdatePatientAsync(patientRequest with { Id = id });
            return Results.NoContent();
        });

        app.MapDelete("/api/patients/{id}", async (Guid id, IPatientManagementService patientService) =>
        {
            var existingPatient = await patientService.GetPatientByIdAsync(id);
            if (existingPatient == null)
            {
                return Results.NotFound();
            }

            await patientService.DeletePatientAsync(id);
            return Results.NoContent();
        });
        return app;
    }
}