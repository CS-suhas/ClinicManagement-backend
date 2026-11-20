namespace ClinicManagement.Application.Models;

/// <summary>Reports API availability and current resource totals.</summary>
public record StatusResponse(
    string Status,
    int Clinics,
    int Providers,
    int Patients,
    int Appointments
);
