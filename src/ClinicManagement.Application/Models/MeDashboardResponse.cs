using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public sealed record MeDashboardResponse(
    AdminDashboardSection? Admin,
    ClinicAdminDashboardSection? ClinicAdmin,
    DoctorDashboardSection? Doctor,
    PatientDashboardSection? Patient);

public sealed record AdminDashboardSection(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount);

public sealed record ClinicAdminDashboardSection(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount);

public sealed record DoctorDashboardSection(
    int TodayAppointmentCount,
    int WeekAppointmentCount,
    int DistinctPatientCount,
    AppointmentListResponse? NextAppointment,
    int PendingRequestCount);

public sealed record PatientDashboardSection(
    AppointmentListResponse? NextAppointment,
    int UpcomingAppointmentCount,
    IReadOnlyList<PatientRecentDecision> RecentDecisions);

public sealed record PatientRecentDecision(
    Guid AppointmentId,
    AppointmentStatus Status,
    DateTimeOffset DecidedAtUtc,
    string ProviderName);

public sealed record DashboardScopeCounts(
    int ClinicCount,
    int ProviderCount,
    int PatientCount,
    int PendingProviderVerificationCount,
    int TodayAppointmentCount);
