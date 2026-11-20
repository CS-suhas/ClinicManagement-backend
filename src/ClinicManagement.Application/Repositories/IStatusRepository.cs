using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface IStatusRepository
{
    Task<StatusStatistics> GetStatisticsAsync();
}
