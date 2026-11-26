using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using Dapper;
using ClinicManagement.Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    private const int UniqueConstraintViolation = 1;

    private const string SelectUser = """
        SELECT Id, IdentitySubject, Email, DisplayName
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
            INSERT INTO Users (Id, IdentitySubject, Email, DisplayName)
            VALUES (:Id, :IdentitySubject, :Email, :DisplayName)
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

    public async Task<UserAccessContext> GetAccessContextAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        // Patients.UserId and Providers.UserId are unique, so the result has one row per administered clinic,
        // or a single row with a null ClinicId when the user administers none. Assigned roles are few and short,
        // so they come back as one comma-separated value rather than multiplying the rows.
        const string sql = """
            SELECT
                (SELECT LISTAGG(r.Role, ',') WITHIN GROUP (ORDER BY r.Role)
                 FROM UserRoles r WHERE r.UserId = u.Id) AS AssignedRoles,
                p.Id AS PatientId,
                pr.Id AS ProviderId,
                pr.VerificationStatus AS ProviderStatus,
                ca.ClinicId
            FROM Users u
            LEFT JOIN Patients p ON p.UserId = u.Id
            LEFT JOIN Providers pr ON pr.UserId = u.Id
            LEFT JOIN ClinicAdmins ca ON ca.UserId = u.Id
            WHERE u.Id = :UserId
            ORDER BY ca.ClinicId
            """;
        var rows = (await connection.QueryAsync<AccessContextRow>(sql, new { UserId = userId.ToString() })).ToList();
        if (rows.Count == 0)
            return UserAccessContext.Empty(userId);

        var first = rows[0];
        return new UserAccessContext(
            userId,
            first.AssignedRoles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [],
            first.PatientId is null ? null : Guid.Parse(first.PatientId),
            first.ProviderId is null ? null : Guid.Parse(first.ProviderId),
            first.ProviderStatus is null ? null : Enum.Parse<ProviderVerificationStatus>(first.ProviderStatus),
            rows.Where(row => row.ClinicId is not null).Select(row => Guid.Parse(row.ClinicId!)).Distinct().ToList());
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

    private sealed record AccessContextRow(
        string? AssignedRoles,
        string? PatientId,
        string? ProviderId,
        string? ProviderStatus,
        string? ClinicId);

    private sealed record UserRow(
        string Id,
        string IdentitySubject,
        string? Email,
        string? DisplayName)
    {
        public User ToEntity() => User.Create(Guid.Parse(Id), IdentitySubject, Email, DisplayName);
    }
}
