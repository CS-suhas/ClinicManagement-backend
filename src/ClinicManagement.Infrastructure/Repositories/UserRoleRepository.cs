using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using Dapper;
using ClinicManagement.Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class UserRoleRepository(IDbConnectionFactory connectionFactory) : IUserRoleRepository
{
    private const int UniqueConstraintViolation = 1;

    public async Task<bool> AssignAsync(Guid userId, string role, Guid? grantedByUserId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO UserRoles (UserId, Role, GrantedByUserId)
            SELECT :UserId, :Role, :GrantedByUserId FROM DUAL
            WHERE NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId = :UserId AND Role = :Role)
            """;
        try
        {
            return await connection.ExecuteAsync(sql, new
            {
                UserId = userId.ToString(),
                Role = role,
                GrantedByUserId = grantedByUserId?.ToString()
            }) > 0;
        }
        catch (OracleException exception) when (exception.Number == UniqueConstraintViolation)
        {
            // A concurrent grant of the same role won; the user holds it either way.
            return false;
        }
    }

    public async Task<bool> RevokeAsync(Guid userId, string role)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM UserRoles WHERE UserId = :UserId AND Role = :Role";
        return await connection.ExecuteAsync(sql, new { UserId = userId.ToString(), Role = role }) > 0;
    }

    public async Task<IReadOnlyList<UserRoleAssignment>> ListAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT u.Id AS UserId, u.IdentitySubject, u.Email, u.DisplayName, r.Role, r.GrantedAtUtc, r.GrantedByUserId
            FROM UserRoles r
            JOIN Users u ON u.Id = r.UserId
            ORDER BY r.Role, u.IdentitySubject
            """;
        var rows = await connection.QueryAsync<AssignmentRow>(sql);
        return rows.Select(row => new UserRoleAssignment(
                Guid.Parse(row.UserId),
                row.IdentitySubject,
                row.Email,
                row.DisplayName,
                row.Role,
                row.GrantedAtUtc,
                row.GrantedByUserId is null ? null : Guid.Parse(row.GrantedByUserId)))
            .ToList();
    }

    private sealed class AssignmentRow
    {
        public string UserId { get; set; } = string.Empty;
        public string IdentitySubject { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTimeOffset GrantedAtUtc { get; set; }
        public string? GrantedByUserId { get; set; }
    }
}
