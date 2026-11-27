using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface IStatusRepository
{
    Task<StatusStatistics> GetStatisticsAsync();
    Task<DashboardResponse> GetDashboardAsync(DateTimeOffset now);
    Task<DashboardScopeCounts> GetPlatformCountsAsync(DateTimeOffset todayStart, DateTimeOffset todayEnd);
    Task<DashboardScopeCounts> GetClinicCountsAsync(
        IReadOnlyCollection<Guid> clinicIds,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd);
    Task<DoctorDashboardSection> GetDoctorDashboardAsync(
        Guid providerId,
        DateTimeOffset now,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd,
        DateTimeOffset weekEnd);
    Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now);
}
