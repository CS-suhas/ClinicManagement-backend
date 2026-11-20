using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface IStatusService
{
    Task<StatusResponse> GetStatusAsync();
    Task<DashboardResponse> GetDashboardAsync();
}
