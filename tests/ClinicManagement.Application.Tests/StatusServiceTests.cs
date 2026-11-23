using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Services;

namespace ClinicManagement.Application.Tests;

public class StatusServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ReturnsStatusAndSelectedResourceCounts()
    {
        var service = new StatusService(
            new FakeStatusRepository(new StatusStatistics(2, 5, 11, 23)),
            TimeProvider.System);

        var result = await service.GetStatusAsync();

        Assert.Equal("ok", result.Status);
        Assert.Equal(2, result.Clinics);
        Assert.Equal(5, result.Providers);
        Assert.Equal(11, result.Patients);
        Assert.Equal(23, result.Appointments);
    }

    private sealed class FakeStatusRepository(StatusStatistics statistics) : IStatusRepository
    {
        public Task<StatusStatistics> GetStatisticsAsync() => Task.FromResult(statistics);
        public Task<DashboardResponse> GetDashboardAsync(DateTimeOffset now) =>
            throw new NotSupportedException();
        public Task<DashboardScopeCounts> GetPlatformCountsAsync(DateTimeOffset todayStart, DateTimeOffset todayEnd) =>
            throw new NotSupportedException();
        public Task<DashboardScopeCounts> GetClinicCountsAsync(
            IReadOnlyCollection<Guid> clinicIds,
            DateTimeOffset todayStart,
            DateTimeOffset todayEnd) => throw new NotSupportedException();
        public Task<int> CountPendingJoinRequestsAsync(IReadOnlyCollection<Guid> clinicIds) =>
            throw new NotSupportedException();
        public Task<DoctorDashboardSection> GetDoctorDashboardAsync(
            Guid providerId,
            DateTimeOffset now,
            DateTimeOffset todayStart,
            DateTimeOffset todayEnd,
            DateTimeOffset weekEnd) => throw new NotSupportedException();
        public Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now) =>
            throw new NotSupportedException();
    }
}
