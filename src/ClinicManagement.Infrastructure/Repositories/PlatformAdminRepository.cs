using ClinicManagement.Application.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class PlatformAdminRepository(IDbConnectionFactory connectionFactory) : IPlatformAdminRepository
{
    public async Task<bool> IsAdminAsync(string identitySubject)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM PlatformAdmins WHERE IdentitySubject = :IdentitySubject";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { IdentitySubject = identitySubject });
        return count > 0;
    }
}
