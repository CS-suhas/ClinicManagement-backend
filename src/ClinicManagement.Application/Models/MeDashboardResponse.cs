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
    int TodayAppointmentCount,
    int PendingJoinRequestCount);

/// <summary>The same counts as the admin section, scoped to the clinics the caller administers, plus join requests.</summary>
public sealed record ClinicAdminDashboardSection(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount,
    int PendingJoinRequestCount);

/// <summary>PendingRequestCount counts REQUESTED appointments starting now or later.</summary>
public sealed record DoctorDashboardSection(
    int TodayAppointmentCount,
    int WeekAppointmentCount,
    int DistinctPatientCount,
    AppointmentListResponse? NextAppointment,
    int PendingRequestCount);

/// <summary>RecentDecisions holds up to five CONFIRMED or DECLINED appointments, newest decision first.</summary>
public sealed record PatientDashboardSection(
    AppointmentListResponse? NextAppointment,
    int UpcomingAppointmentCount,
    IReadOnlyList<PatientRecentDecision> RecentDecisions);

/// <summary>A confirmation or decline of one of the patient's appointments. DecidedAtUtc is the last status change.</summary>
public sealed record PatientRecentDecision(
    Guid AppointmentId,
    string Status,
    DateTimeOffset DecidedAtUtc,
    string ProviderName);

/// <summary>Counts over the whole platform, or over a set of clinics.</summary>
public sealed record DashboardScopeCounts(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount);
