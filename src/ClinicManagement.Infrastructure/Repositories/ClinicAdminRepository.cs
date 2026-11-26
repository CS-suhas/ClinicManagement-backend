using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using Dapper;
using ClinicManagement.Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class ClinicAdminRepository(IDbConnectionFactory connectionFactory) : IClinicAdminRepository
{
    public async Task<bool> IsAdminOfClinicAsync(Guid userId, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM ClinicAdmins WHERE ClinicId = :ClinicId AND UserId = :UserId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            ClinicId = clinicId.ToString(),
            UserId = userId.ToString()
        });
        return count > 0;
    }

    public async Task<IReadOnlyList<Guid>> GetClinicIdsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT ClinicId FROM ClinicAdmins WHERE UserId = :UserId ORDER BY ClinicId";
        var clinicIds = await connection.QueryAsync<string>(sql, new { UserId = userId.ToString() });
        return clinicIds.Select(Guid.Parse).ToList();
    }

    public async Task AddAsync(ClinicAdmin clinicAdmin)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "INSERT INTO ClinicAdmins (Id, ClinicId, UserId) VALUES (:Id, :ClinicId, :UserId)";
        await connection.ExecuteAsync(sql, new
        {
            Id = clinicAdmin.Id.ToString(),
            ClinicId = clinicAdmin.ClinicId.ToString(),
            UserId = clinicAdmin.UserId.ToString()
        });
    }
}
