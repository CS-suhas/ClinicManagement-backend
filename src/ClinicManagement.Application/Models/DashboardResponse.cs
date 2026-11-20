namespace ClinicManagement.Application.Models;

/// <summary>Counts and the next appointments used by the dashboard.</summary>
public sealed record DashboardResponse(
    int Clinics,
    int Providers,
    int Patients,
    int AllergyTypes,
    int Appointments,
    IReadOnlyList<AppointmentListResponse> UpcomingAppointments);
