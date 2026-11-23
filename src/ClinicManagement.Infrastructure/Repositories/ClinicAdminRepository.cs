using ClinicManagement.Application.Repositories;
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
}
