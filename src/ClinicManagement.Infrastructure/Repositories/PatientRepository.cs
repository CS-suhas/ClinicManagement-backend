using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using Dapper;
using ClinicManagement.Infrastructure.Data;
using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Infrastructure.Repositories;

public class PatientRepository(IDbConnectionFactory connectionFactory) : IPatientRepository
{
    public async Task<Patient?> GetPatientByIdAsync(Guid patientId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, FirstName, LastName, DateOfBirth, PhoneNumber, Email, CreatedAtUtc, UpdatedAtUtc FROM Patients WHERE Id = :Id";
        return await connection.QuerySingleOrDefaultAsync<Patient>(sql, new { Id = patientId.ToString() });
    }

    public async Task<IEnumerable<Patient>> GetAllPatientsAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, FirstName, LastName, DateOfBirth, PhoneNumber, Email, CreatedAtUtc, UpdatedAtUtc FROM Patients";
        return await connection.QueryAsync<Patient>(sql);
    }

    public async Task<Guid> AddPatientAsync(Patient patient)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "INSERT INTO Patients (Id, FirstName, LastName, DateOfBirth, PhoneNumber, Email, UserId) VALUES (:Id, :FirstName, :LastName, :DateOfBirth, :PhoneNumber, :Email, :UserId)";
        await connection.ExecuteAsync(sql, new
        {
            Id = patient.Id.ToString(),
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.PhoneNumber,
            patient.Email,
            UserId = patient.UserId?.ToString()
        });
        return patient.Id;
    }

    public async Task<Guid> UpdatePatientAsync(Patient patient)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "UPDATE Patients SET FirstName = :FirstName, LastName = :LastName, DateOfBirth = :DateOfBirth, PhoneNumber = :PhoneNumber, Email = :Email, UpdatedAtUtc = SYSTIMESTAMP WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new
        {
            Id = patient.Id.ToString(),
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.PhoneNumber,
            patient.Email
        });
        return patient.Id;
    }

    public async Task<Guid> DeletePatientAsync(Guid patientId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM Patients WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = patientId.ToString() });
        return patientId;
    }

    public async Task<bool> PatientExistsAsync(Guid patientId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Patients WHERE Id = :Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = patientId.ToString() });
        return count > 0;
    }

    public async Task<bool> HasDependentRecordsAsync(Guid patientId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT COUNT(1)
            FROM (
                SELECT PatientId FROM PatientAllergies WHERE PatientId = :Id
                UNION ALL
                SELECT PatientId FROM Appointments WHERE PatientId = :Id
            )
            """;
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = patientId.ToString() });
        return count > 0;
    }

    public async Task<IEnumerable<PatientAllergy>> GetAllergiesByPatientIdAsync(Guid patientId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT pa.Id, pa.PatientId, pa.AllergyId, pa.Severity, pa.Notes,
                   a.Id, a.Name
            FROM PatientAllergies pa
            INNER JOIN AllergyTypes a ON a.Id = pa.AllergyId
            WHERE pa.PatientId = :Id
            ORDER BY a.Name
            """;
        return await connection.QueryAsync<PatientAllergy, AllergyType, PatientAllergy>(
            sql,
            (patientAllergy, allergyType) =>
            {
                patientAllergy.SetAllergyType(allergyType);
                return patientAllergy;
            },
            new { Id = patientId.ToString() },
            splitOn: "Id");
    }

    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(
        PageQuery query,
        int offset)
    {
        using var connection = connectionFactory.CreateConnection();
        const string where = """
            WHERE (:Search IS NULL
                OR UPPER(FirstName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(FirstName || ' ' || LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(Email) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(PhoneNumber) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\')
            """;
        var parameters = new { Search = SqlLike.Escape(query.NormalizedSearch), Offset = offset, query.PageSize };
        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM Patients {where}",
            parameters);
        var items = (await connection.QueryAsync<Patient>(
            $"""
            SELECT Id, FirstName, LastName, DateOfBirth, PhoneNumber, Email, CreatedAtUtc, UpdatedAtUtc
            FROM Patients
            {where}
            ORDER BY LastName, FirstName, Id
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """,
            parameters)).AsList();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<PatientAllergy> Items, int TotalCount)> SearchPatientAllergiesAsync(
        Guid patientId,
        PageQuery query,
        int offset)
    {
        using var connection = connectionFactory.CreateConnection();
        const string where = """
            WHERE pa.PatientId = :PatientId
              AND (:Search IS NULL
                OR UPPER(a.Name) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(pa.Severity) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                OR UPPER(pa.Notes) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\')
            """;
        var parameters = new
        {
            PatientId = patientId.ToString(),
            Search = SqlLike.Escape(query.NormalizedSearch),
            Offset = offset,
            query.PageSize
        };
        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM PatientAllergies pa INNER JOIN AllergyTypes a ON a.Id = pa.AllergyId {where}",
            parameters);
        var items = (await connection.QueryAsync<PatientAllergy, AllergyType, PatientAllergy>(
            $"""
            SELECT pa.Id, pa.PatientId, pa.AllergyId, pa.Severity, pa.Notes,
                   a.Id, a.Name
            FROM PatientAllergies pa
            INNER JOIN AllergyTypes a ON a.Id = pa.AllergyId
            {where}
            ORDER BY a.Name, pa.Id
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """,
            (patientAllergy, allergyType) =>
            {
                patientAllergy.SetAllergyType(allergyType);
                return patientAllergy;
            },
            parameters,
            splitOn: "Id")).AsList();
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> GetSelectableAllergyTypesAsync(
        Guid patientId,
        Guid? selectedAllergyId,
        PageQuery query,
        int offset)
    {
        using var connection = connectionFactory.CreateConnection();
        const string where = """
            FROM AllergyTypes a
            WHERE NOT EXISTS (
                SELECT 1
                FROM PatientAllergies pa
                WHERE pa.PatientId = :PatientId
                  AND pa.AllergyId = a.Id
                  AND (:SelectedAllergyId IS NULL OR pa.AllergyId <> :SelectedAllergyId)
            )
              AND (:Search IS NULL OR UPPER(a.Name) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\')
            """;
        var parameters = new
        {
            PatientId = patientId.ToString(),
            SelectedAllergyId = selectedAllergyId?.ToString(),
            Search = SqlLike.Escape(query.NormalizedSearch),
            Offset = offset,
            query.PageSize
        };
        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) {where}",
            parameters);
        var items = (await connection.QueryAsync<AllergyType>(
            $"""
            SELECT a.Id, a.Name, a.CreatedAtUtc, a.UpdatedAtUtc
            {where}
            ORDER BY a.Name, a.Id
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """,
            parameters)).AsList();
        return (items, totalCount);
    }

    public async Task<Guid> UpsertPatientAllergyAsync(
        Guid patientId,
        Guid allergyId,
        AllergySeverity severity,
        string? notes)
    {
        using var connection = connectionFactory.CreateConnection();
        var id = Guid.NewGuid();
        const string mergeSql = """
            MERGE INTO PatientAllergies existing
            USING (SELECT :PatientId PatientId, :AllergyId AllergyId FROM DUAL) requested
            ON (existing.PatientId = requested.PatientId AND existing.AllergyId = requested.AllergyId)
            WHEN MATCHED THEN UPDATE SET
                Severity = :Severity,
                Notes = :Notes,
                UpdatedAtUtc = SYSTIMESTAMP
            WHEN NOT MATCHED THEN INSERT
                (Id, PatientId, AllergyId, Severity, Notes)
                VALUES (:Id, :PatientId, :AllergyId, :Severity, :Notes)
            """;
        await connection.ExecuteAsync(mergeSql, new
        {
            Id = id.ToString(),
            PatientId = patientId.ToString(),
            AllergyId = allergyId.ToString(),
            Severity = severity.ToString(),
            notes
        });
        const string selectSql = """
            SELECT Id
            FROM PatientAllergies
            WHERE PatientId = :PatientId AND AllergyId = :AllergyId
            """;
        var storedId = await connection.ExecuteScalarAsync<string?>(selectSql, new
        {
            PatientId = patientId.ToString(),
            AllergyId = allergyId.ToString()
        });
        if (!Guid.TryParse(storedId, out var storedGuid))
            throw new InvalidOperationException("The saved patient allergy could not be retrieved.");
        return storedGuid;
    }

    public async Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM PatientAllergies WHERE PatientId = :PatientId AND AllergyId = :AllergyId";
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            PatientId = patientId.ToString(),
            AllergyId = allergyId.ToString()
        });
        return rowsAffected == 0 ? Guid.Empty : allergyId;
    }

    public async Task<IReadOnlyDictionary<Guid, PatientAllergySummary>> GetAllergySummariesAsync(
        IReadOnlyCollection<Guid>? patientIds)
    {
        if (patientIds is { Count: 0 })
            return new Dictionary<Guid, PatientAllergySummary>();

        using var connection = connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        var filter = string.Empty;
        if (patientIds is not null)
        {
            filter = "WHERE PatientId IN :PatientIds";
            parameters.Add("PatientIds", patientIds.Select(id => id.ToString()).ToArray());
        }

        var rows = await connection.QueryAsync<AllergySummaryRow>(
            $"""
            SELECT PatientId,
                   COUNT(1) AS AllergyCount,
                   MAX(CASE Severity WHEN 'SEVERE' THEN 3 WHEN 'MODERATE' THEN 2 WHEN 'MILD' THEN 1 ELSE 0 END) AS SeverityRank
            FROM PatientAllergies
            {filter}
            GROUP BY PatientId
            """,
            parameters);
        return rows.ToDictionary(
            row => row.PatientId,
            row => new PatientAllergySummary(checked((int)row.AllergyCount), SeverityName(row.SeverityRank)));
    }

    /// <summary>Maps the rank used by the SQL severity summaries back to its name.</summary>
    internal static string? SeverityName(decimal? rank) => rank switch
    {
        3 => "SEVERE",
        2 => "MODERATE",
        1 => "MILD",
        _ => null
    };

    private sealed record AllergySummaryRow(Guid PatientId, decimal AllergyCount, decimal? SeverityRank);

    public async Task<Patient?> GetByUserIdAsync(Guid userId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, FirstName, LastName, DateOfBirth, PhoneNumber, Email, UserId, CreatedAtUtc, UpdatedAtUtc FROM Patients WHERE UserId = :UserId";
        return await connection.QuerySingleOrDefaultAsync<Patient>(sql, new { UserId = userId.ToString() });
    }
}