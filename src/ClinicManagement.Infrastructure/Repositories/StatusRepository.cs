using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class StatusRepository(IDbConnectionFactory connectionFactory) : IStatusRepository
{
    public async Task<StatusStatistics> GetStatisticsAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM Clinics) AS Clinics,
                (SELECT COUNT(1) FROM Providers) AS Providers,
                (SELECT COUNT(1) FROM Patients) AS Patients,
                (SELECT COUNT(1) FROM Appointments) AS Appointments
            FROM DUAL
            """;
        return await connection.QuerySingleAsync<StatusStatistics>(sql);
    }
}
