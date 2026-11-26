using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Infrastructure.Data;
using Dapper;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Infrastructure.Repositories;

public class ProviderRepository(IDbConnectionFactory connectionFactory) : IProviderRepository
{
    private const string SelectProviderWithClinic = """
        SELECT pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId, pr.VerificationStatus, pr.UserId, pr.Fee, pr.Gender,
               c.Id, c.Name, c.Address, c.PhoneNumber
        FROM Providers pr
        LEFT JOIN Clinics c ON c.Id = pr.ClinicId
        """;

    public async Task<Guid> AddProviderAsync(Provider provider, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO Providers (Id, FirstName, LastName, Specialty, ClinicId, VerificationStatus, UserId, Fee, Gender)
            VALUES (:Id, :FirstName, :LastName, :Specialty, :ClinicId, :VerificationStatus, :UserId, :Fee, :Gender)
            """;
        await connection.ExecuteAsync(sql, new
        {
            Id = provider.Id.ToString(),
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            ClinicId = clinicId.ToString(),
            VerificationStatus = provider.VerificationStatus.ToString(),
            UserId = provider.UserId?.ToString(),
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

    public async Task<IReadOnlyDictionary<Guid, int>> GetVerifiedProviderCountsByClinicAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT ClinicId, COUNT(1) AS ProviderCount
            FROM Providers
            WHERE VerificationStatus = 'VERIFIED'
            GROUP BY ClinicId
            """;
        var rows = await connection.QueryAsync<ClinicProviderCountRow>(sql);
        return rows.ToDictionary(row => row.ClinicId, row => checked((int)row.ProviderCount));
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
            ? "AND pr.VerificationStatus = 'VERIFIED'"
            : string.Empty;
        var where = $"""
            WHERE (:ClinicId IS NULL OR pr.ClinicId = :ClinicId)
              AND (:VerificationStatus IS NULL OR pr.VerificationStatus = :VerificationStatus)
              {visibility}
              AND (:Search IS NULL
                OR UPPER(pr.FirstName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(pr.LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(pr.Specialty) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(c.Name) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\')
            """;
        var parameters = new
        {
            ClinicId = clinicId?.ToString(),
            VerificationStatus = verificationStatus?.ToString(),
            Search = SqlLike.Escape(query.NormalizedSearch),
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

    public async Task<(IReadOnlyList<ProviderPatientRosterItem> Items, int TotalCount)> SearchPatientRosterAsync(
        Guid providerId,
        PageQuery query,
        int offset,
        DateTimeOffset now)
    {
        using var connection = connectionFactory.CreateConnection();
        const string patientFilter = """
            WHERE EXISTS (SELECT 1 FROM Appointments va WHERE va.PatientId = p.Id AND va.ProviderId = :ProviderId)
              AND (:Search IS NULL
                OR UPPER(p.FirstName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(p.LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(p.FirstName || ' ' || p.LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\')
            """;
        const string patientJoin = """
            FROM Patients p
            INNER JOIN (
                SELECT PatientId, COUNT(1) AS AppointmentCount
                FROM Appointments
                WHERE ProviderId = :ProviderId
                GROUP BY PatientId
            ) counts ON counts.PatientId = p.Id
            LEFT JOIN (
                SELECT PatientId,
                       COUNT(1) AS AllergyCount,
                       MAX(CASE Severity WHEN 'SEVERE' THEN 3 WHEN 'MODERATE' THEN 2 WHEN 'MILD' THEN 1 ELSE 0 END) AS SeverityRank
                FROM PatientAllergies
                GROUP BY PatientId
            ) allergySummary ON allergySummary.PatientId = p.Id
            """;
        var parameters = new
        {
            ProviderId = providerId.ToString(),
            Search = SqlLike.Escape(query.NormalizedSearch),
            Offset = offset,
            query.PageSize
        };

        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM Patients p {patientFilter}",
            parameters);
        var patientRows = (await connection.QueryAsync<RosterPatientRow>(
            $"""
            SELECT p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.Email, p.PhoneNumber, counts.AppointmentCount,
                   NVL(allergySummary.AllergyCount, 0) AS AllergyCount,
                   CASE allergySummary.SeverityRank
                       WHEN 3 THEN 'SEVERE'
                       WHEN 2 THEN 'MODERATE'
                       WHEN 1 THEN 'MILD'
                   END AS HighestSeverity
            {patientJoin}
            {patientFilter}
            ORDER BY p.LastName, p.FirstName, p.Id
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """,
            parameters)).AsList();
        if (patientRows.Count == 0)
            return ([], totalCount);

        // One query finds the latest past and the earliest upcoming visit for every patient on the page.
        var visitRows = (await connection.QueryAsync<RosterVisitRow>(
            """
            SELECT Kind, PatientId, Id, AppointmentDateTime, ReasonForVisit
            FROM (
                SELECT 'LAST' AS Kind, PatientId, Id, AppointmentDateTime, ReasonForVisit,
                       ROW_NUMBER() OVER (PARTITION BY PatientId ORDER BY AppointmentDateTime DESC, Id) AS RankNo
                FROM Appointments
                WHERE ProviderId = :ProviderId
                  AND PatientId IN :PatientIds
                  AND AppointmentDateTime < :Now
                  AND Status NOT IN ('DECLINED', 'CANCELLED')
                UNION ALL
                SELECT 'NEXT' AS Kind, PatientId, Id, AppointmentDateTime, ReasonForVisit,
                       ROW_NUMBER() OVER (PARTITION BY PatientId ORDER BY AppointmentDateTime, Id) AS RankNo
                FROM Appointments
                WHERE ProviderId = :ProviderId
                  AND PatientId IN :PatientIds
                  AND AppointmentDateTime >= :Now
                  AND Status NOT IN ('DECLINED', 'CANCELLED')
            )
            WHERE RankNo = 1
            """,
            new
            {
                ProviderId = providerId.ToString(),
                PatientIds = patientRows.Select(row => row.Id.ToString()).ToArray(),
                Now = now
            })).AsList();

        var lastVisits = visitRows.Where(visit => visit.Kind == "LAST").ToDictionary(visit => visit.PatientId);
        var nextVisits = visitRows.Where(visit => visit.Kind == "NEXT").ToDictionary(visit => visit.PatientId);
        var items = patientRows.Select(row => new ProviderPatientRosterItem(
            new RosterPatient(
                row.Id,
                row.FirstName,
                row.LastName,
                row.DateOfBirth,
                row.Email,
                row.PhoneNumber,
                checked((int)row.AllergyCount),
                row.HighestSeverity),
            lastVisits.TryGetValue(row.Id, out var last) ? ToRosterVisit(last) : null,
            nextVisits.TryGetValue(row.Id, out var next) ? ToRosterVisit(next) : null,
            checked((int)row.AppointmentCount))).ToList();
        return (items, totalCount);
    }

    public async Task<Provider?> GetByUserIdAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        var providers = await QueryProvidersAsync(
            connection,
            $"{SelectProviderWithClinic} WHERE pr.UserId = :UserId",
            new { UserId = userId.ToString() });
        return providers.SingleOrDefault();
    }

    private sealed record RosterPatientRow(
        Guid Id,
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string Email,
        string PhoneNumber,
        decimal AppointmentCount,
        decimal AllergyCount,
        string? HighestSeverity);

    private sealed record RosterVisitRow(
        string Kind,
        Guid PatientId,
        Guid Id,
        DateTimeOffset AppointmentDateTime,
        string? ReasonForVisit);

    private static RosterVisit ToRosterVisit(RosterVisitRow visit) =>
        new(visit.Id, visit.AppointmentDateTime, visit.ReasonForVisit);

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

    private sealed record ClinicProviderCountRow(Guid ClinicId, decimal ProviderCount);
}
