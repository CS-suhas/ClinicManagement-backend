using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface IMeDashboardService
{
    Task<MeDashboardResponse> GetDashboardAsync();
}
