namespace ClinicManagement.Application.Models;

public record StatusStatistics(
    int Clinics,
    int Providers,
    int Patients,
    int Appointments
);
