using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Application.Providers.Interfaces;
using Infrastructure.Data;
using Dapper;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Infrastructure.Repositories;

public class ProviderRepository(IDbConnectionFactory connectionFactory) : IProviderRepository
{
    // Providers without a clinic (self-registered, not yet approved anywhere) must still be returned,
    // so the clinic is a LEFT JOIN and may map to null.
    private const string SelectProviderWithClinic = """
        SELECT pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId, pr.VerificationStatus, pr.IdentitySubject, pr.Fee, pr.Gender,
               c.Id, c.Name, c.Address, c.PhoneNumber
        FROM Providers pr
        LEFT JOIN Clinics c ON c.Id = pr.ClinicId
        """;

    public async Task<Guid> AddProviderAsync(Provider provider, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO Providers (Id, FirstName, LastName, Specialty, ClinicId, VerificationStatus, IdentitySubject, Fee, Gender)
            VALUES (:Id, :FirstName, :LastName, :Specialty, :ClinicId, :VerificationStatus, :IdentitySubject, :Fee, :Gender)
            """;
        await connection.ExecuteAsync(sql, new
        {
            Id = provider.Id.ToString(),
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            ClinicId = clinicId.ToString(),
            VerificationStatus = provider.VerificationStatus.ToString(),
            provider.IdentitySubject,
            provider.Fee,
            Gender = provider.Gender?.ToString()
        });
        return provider.Id;
    }

    public async Task<Guid> AddRegistrationAsync(Provider provider)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO Providers (Id, FirstName, LastName, Specialty, ClinicId, VerificationStatus, IdentitySubject, Fee, Gender)
            VALUES (:Id, :FirstName, :LastName, :Specialty, NULL, :VerificationStatus, :IdentitySubject, :Fee, :Gender)
            """;
        await connection.ExecuteAsync(sql, new
        {
            Id = provider.Id.ToString(),
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            VerificationStatus = provider.VerificationStatus.ToString(),
            provider.IdentitySubject,
            provider.Fee,
            Gender = provider.Gender?.ToString()
        });
        return provider.Id;
    }

    public async Task<bool> TryUpdateVerificationStatusAsync(
        Guid providerId,
        ProviderVerificationStatus expected,
        ProviderVerificationStatus next)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            UPDATE Providers
            SET VerificationStatus = :Next, UpdatedAtUtc = SYSTIMESTAMP
            WHERE Id = :Id AND VerificationStatus = :Expected
            """;
        var rows = await connection.ExecuteAsync(sql, new
        {
            Id = providerId.ToString(),
            Expected = expected.ToString(),
            Next = next.ToString()
        });
        return rows == 1;
    }

    public async Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM Providers WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = providerId.ToString() });
        return providerId;
    }

    public async Task<bool> HasAppointmentsAsync(Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Appointments WHERE ProviderId = :Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = providerId.ToString() });
        return count > 0;
    }

    public async Task<IEnumerable<Provider>> GetAllProvidersAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        return await QueryProvidersAsync(connection, SelectProviderWithClinic);
    }

    public async Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        return await QueryProvidersAsync(
            connection,
            $"""
            {SelectProviderWithClinic}
            WHERE pr.ClinicId = :ClinicId
            ORDER BY pr.LastName, pr.FirstName, pr.Id
            """,
            new { ClinicId = clinicId.ToString() });
    }

    public async Task<Guid> UpdateProviderAsync(Provider provider, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "UPDATE Providers SET FirstName = :FirstName, LastName = :LastName, Specialty = :Specialty, Fee = :Fee, Gender = :Gender, ClinicId = :ClinicId, UpdatedAtUtc = SYSTIMESTAMP WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new
        {
            Id = provider.Id.ToString(),
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            provider.Fee,
            Gender = provider.Gender?.ToString(),
            ClinicId = clinicId.ToString()
        });
        return provider.Id;
    }

    public async Task<Provider?> GetProviderByIdAsync(Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        var providers = await QueryProvidersAsync(
            connection,
            $"{SelectProviderWithClinic} WHERE pr.Id = :Id",
            new { Id = providerId.ToString() });
        return providers.SingleOrDefault();
    }

    public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus = null) =>
        SearchAsync(query, offset, clinicId, verificationStatus, publicOnly: false);

    public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId) =>
        SearchAsync(query, offset, clinicId, verificationStatus: null, publicOnly: true);

    private async Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchAsync(
        PageQuery query,
        int offset,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus,
        bool publicOnly)
    {
        using var connection = connectionFactory.CreateConnection();
        var visibility = publicOnly
            ? "AND pr.VerificationStatus = 'VERIFIED' AND pr.ClinicId IS NOT NULL"
            : string.Empty;
        var where = $"""
            WHERE (:ClinicId IS NULL OR pr.ClinicId = :ClinicId)
              AND (:VerificationStatus IS NULL OR pr.VerificationStatus = :VerificationStatus)
              {visibility}
              AND (:Search IS NULL
                OR UPPER(pr.FirstName) LIKE '%' || UPPER(:Search) || '%'
                OR UPPER(pr.LastName) LIKE '%' || UPPER(:Search) || '%'
                OR UPPER(pr.Specialty) LIKE '%' || UPPER(:Search) || '%'
                OR UPPER(c.Name) LIKE '%' || UPPER(:Search) || '%')
            """;
        var parameters = new
        {
            ClinicId = clinicId?.ToString(),
            VerificationStatus = verificationStatus?.ToString(),
            Search = query.NormalizedSearch,
            Offset = offset,
            query.PageSize
        };
        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM Providers pr LEFT JOIN Clinics c ON c.Id = pr.ClinicId {where}",
            parameters);
        var providers = (await QueryProvidersAsync(
            connection,
            $"""
            {SelectProviderWithClinic}
            {where}
            ORDER BY pr.LastName, pr.FirstName, pr.Id
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """,
            parameters)).AsList();
        return (providers, totalCount);
    }

    public async Task<Provider?> GetByIdentitySubjectAsync(string identitySubject)
    {
        using var connection = connectionFactory.CreateConnection();
        var providers = await QueryProvidersAsync(
            connection,
            $"{SelectProviderWithClinic} WHERE pr.IdentitySubject = :IdentitySubject",
            new { IdentitySubject = identitySubject });
        return providers.SingleOrDefault();
    }

    private static Task<IEnumerable<Provider>> QueryProvidersAsync(
        System.Data.IDbConnection connection,
        string sql,
        object? parameters = null) =>
        connection.QueryAsync<Provider, Clinic?, Provider>(
            sql,
            (provider, clinic) =>
            {
                if (clinic is not null)
                    provider.SetClinic(clinic);
                return provider;
            },
            parameters,
            splitOn: "Id");
}
