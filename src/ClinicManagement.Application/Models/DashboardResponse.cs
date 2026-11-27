namespace ClinicManagement.Application.Models;

public sealed record DashboardResponse(
    int Clinics,
    int Providers,
    int Patients,
    int AllergyTypes,
    int Appointments,
    IReadOnlyList<AppointmentListResponse> UpcomingAppointments);
