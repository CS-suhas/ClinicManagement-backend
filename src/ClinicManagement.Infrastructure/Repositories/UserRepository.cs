using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using Dapper;
using Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    private const int UniqueConstraintViolation = 1;

    private const string SelectUser = """
        SELECT Id, IdentitySubject, Email, DisplayName, IsPlatformAdmin
        FROM Users
        """;

    public async Task<User?> GetBySubjectAsync(string identitySubject)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            $"{SelectUser} WHERE IdentitySubject = :IdentitySubject",
            new { IdentitySubject = identitySubject });
        return row?.ToEntity();
    }

    public async Task<User> GetOrCreateAsync(string identitySubject, string? email = null, string? displayName = null)
    {
        var existing = await GetBySubjectAsync(identitySubject);
        if (existing is not null)
        {
            if (existing.UpdateProfile(email, displayName))
                await UpdateProfileAsync(existing);
            return existing;
        }

        var user = User.Create(Guid.NewGuid(), identitySubject, email, displayName);
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO Users (Id, IdentitySubject, Email, DisplayName, IsPlatformAdmin)
            VALUES (:Id, :IdentitySubject, :Email, :DisplayName, 0)
            """;
        try
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = user.Id.ToString(),
                user.IdentitySubject,
                user.Email,
                user.DisplayName
            });
            return user;
        }
        catch (OracleException exception) when (exception.Number == UniqueConstraintViolation)
        {
            // Lost a race with a concurrent first request for the same subject; theirs won.
            return await GetBySubjectAsync(identitySubject)
                ?? throw new InvalidOperationException($"User '{identitySubject}' vanished after a duplicate insert.");
        }
    }

    public async Task<UserRoleFacts> GetRoleFactsAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM Patients p WHERE p.UserId = u.Id) AS PatientCount,
                -- Providers.UserId is unique, so MAX only collapses zero-or-one row.
                (SELECT MAX(pr.VerificationStatus) FROM Providers pr WHERE pr.UserId = u.Id) AS ProviderStatus,
                (SELECT COUNT(1) FROM ClinicAdmins ca WHERE ca.UserId = u.Id) AS ClinicAdminCount,
                u.IsPlatformAdmin
            FROM Users u
            WHERE u.Id = :UserId
            """;
        var row = await connection.QuerySingleOrDefaultAsync<RoleFactsRow>(sql, new { UserId = userId.ToString() });
        if (row is null)
            return new UserRoleFacts(false, null, false, false);

        return new UserRoleFacts(
            row.PatientCount > 0,
            row.ProviderStatus is null ? null : Enum.Parse<ProviderVerificationStatus>(row.ProviderStatus),
            row.ClinicAdminCount > 0,
            row.IsPlatformAdmin != 0);
    }

    private async Task UpdateProfileAsync(User user)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            UPDATE Users
            SET Email = :Email, DisplayName = :DisplayName, UpdatedAtUtc = SYSTIMESTAMP
            WHERE Id = :Id
            """;
        await connection.ExecuteAsync(sql, new { Id = user.Id.ToString(), user.Email, user.DisplayName });
    }

    private sealed record RoleFactsRow(decimal PatientCount, string? ProviderStatus, decimal ClinicAdminCount, short IsPlatformAdmin);

    private sealed record UserRow(
        string Id,
        string IdentitySubject,
        string? Email,
        string? DisplayName,
        short IsPlatformAdmin)
    {
        public User ToEntity()
        {
            var user = User.Create(Guid.Parse(Id), IdentitySubject, Email, DisplayName);
            if (IsPlatformAdmin != 0)
                user.GrantPlatformAdmin();
            return user;
        }
    }
}
