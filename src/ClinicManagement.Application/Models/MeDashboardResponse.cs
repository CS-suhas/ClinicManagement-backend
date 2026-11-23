namespace ClinicManagement.Application.Models;

/// <summary>
/// Dashboard data for the signed-in user. A section is filled only when the caller holds that role
/// (or, for doctor and patient, has the linked record) and is null otherwise.
/// </summary>
public sealed record MeDashboardResponse(
    AdminDashboardSection? Admin,
    ClinicAdminDashboardSection? ClinicAdmin,
    DoctorDashboardSection? Doctor,
    PatientDashboardSection? Patient);

/// <summary>Platform-wide counts for administrators.</summary>
public sealed record AdminDashboardSection(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount);

/// <summary>The same counts as the admin section, scoped to the clinics the caller administers, plus join requests.</summary>
public sealed record ClinicAdminDashboardSection(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount,
    int PendingJoinRequestCount);

public sealed record DoctorDashboardSection(
    int TodayAppointmentCount,
    int WeekAppointmentCount,
    int DistinctPatientCount,
    AppointmentListResponse? NextAppointment);

public sealed record PatientDashboardSection(
    AppointmentListResponse? NextAppointment,
    int UpcomingAppointmentCount);

/// <summary>Counts over the whole platform, or over a set of clinics.</summary>
public sealed record DashboardScopeCounts(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount);
