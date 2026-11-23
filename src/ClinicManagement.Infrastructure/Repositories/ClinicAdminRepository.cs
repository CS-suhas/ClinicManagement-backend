using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class ClinicAdminRepository(IDbConnectionFactory connectionFactory) : IClinicAdminRepository
{
    public async Task<bool> IsAdminOfClinicAsync(string identitySubject, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM ClinicAdmins WHERE ClinicId = :ClinicId AND IdentitySubject = :IdentitySubject";
        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            ClinicId = clinicId.ToString(),
            IdentitySubject = identitySubject
        });
        return count > 0;
    }

    public async Task<bool> IsAnyAdminAsync(string identitySubject)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM ClinicAdmins WHERE IdentitySubject = :IdentitySubject";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { IdentitySubject = identitySubject });
        return count > 0;
    }

    public async Task<IReadOnlyList<Guid>> GetClinicIdsAsync(string identitySubject)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT ClinicId FROM ClinicAdmins WHERE IdentitySubject = :IdentitySubject ORDER BY ClinicId";
        var clinicIds = await connection.QueryAsync<string>(sql, new { IdentitySubject = identitySubject });
        return clinicIds.Select(Guid.Parse).ToList();
    }

    public async Task AddAsync(ClinicAdmin clinicAdmin)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "INSERT INTO ClinicAdmins (Id, ClinicId, IdentitySubject) VALUES (:Id, :ClinicId, :IdentitySubject)";
        await connection.ExecuteAsync(sql, new
        {
            Id = clinicAdmin.Id.ToString(),
            ClinicId = clinicAdmin.ClinicId.ToString(),
            clinicAdmin.IdentitySubject
        });
    }
}
