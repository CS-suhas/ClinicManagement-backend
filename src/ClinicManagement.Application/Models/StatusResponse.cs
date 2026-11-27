namespace ClinicManagement.Application.Models;

public record StatusResponse(
    string Status,
    int Clinics,
    int Providers,
    int Patients,
    int Appointments
);
