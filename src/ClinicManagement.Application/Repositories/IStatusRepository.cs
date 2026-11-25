using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface IStatusRepository
{
    Task<StatusStatistics> GetStatisticsAsync();
    Task<DashboardResponse> GetDashboardAsync(DateTimeOffset now);
    /// <summary>Platform-wide counts. "Today" is [todayStart, todayEnd) and excludes declined and cancelled appointments.</summary>
    Task<DashboardScopeCounts> GetPlatformCountsAsync(DateTimeOffset todayStart, DateTimeOffset todayEnd);
    /// <summary>Counts for the clinics in <paramref name="clinicIds"/>. ClinicCount is the number of distinct clinics given.</summary>
    Task<DashboardScopeCounts> GetClinicCountsAsync(
        IReadOnlyCollection<Guid> clinicIds,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd);
    /// <summary>Doctor counts and next active appointment. The week is [todayStart, weekEnd).</summary>
    Task<DoctorDashboardSection> GetDoctorDashboardAsync(
        Guid providerId,
        DateTimeOffset now,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd,
        DateTimeOffset weekEnd);
    Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now);
}
