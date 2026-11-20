using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;

namespace ClinicManagement.Application.Services;

public sealed class StatusService(
    IStatusRepository statusRepository,
    TimeProvider timeProvider) : IStatusService
{
    public async Task<StatusResponse> GetStatusAsync()
    {
        var statistics = await statusRepository.GetStatisticsAsync();
        return new(
            "ok",
            statistics.Clinics,
            statistics.Providers,
            statistics.Patients,
            statistics.Appointments);
    }

    public Task<DashboardResponse> GetDashboardAsync() =>
        statusRepository.GetDashboardAsync(timeProvider.GetUtcNow());
}
