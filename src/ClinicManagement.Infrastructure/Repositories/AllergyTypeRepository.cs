using ClinicManagement.Domain.Entities;
using Infrastructure.Data;
using Dapper;
using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Infrastructure.Repositories;

public class AllergyTypeRepository(IDbConnectionFactory connectionFactory) : IAllergyTypeRepository
{
    public async Task<Guid> AddAllergyTypeAsync(AllergyType allergyType)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "INSERT INTO AllergyTypes (Id, Name) VALUES (:Id, :Name)";
        await connection.ExecuteAsync(sql, new { Id = allergyType.Id.ToString(), allergyType.Name });
        return allergyType.Id;
    }

    public async Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM AllergyTypes WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = allergyTypeId.ToString() });
        return allergyTypeId;
    }

    public async Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Name, CreatedAtUtc, UpdatedAtUtc FROM AllergyTypes";
        return await connection.QueryAsync<AllergyType>(sql);
    }

    public async Task<IEnumerable<Patient>> GetPatientsByAllergyTypeIdAsync(Guid allergyTypeId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   p.CreatedAtUtc, p.UpdatedAtUtc
            FROM Patients p
            INNER JOIN PatientAllergies pa ON pa.PatientId = p.Id
            WHERE pa.AllergyId = :Id
            ORDER BY p.LastName, p.FirstName
            """;
        return await connection.QueryAsync<Patient>(sql, new { Id = allergyTypeId.ToString() });
    }

    public async Task<bool> HasPatientsWithAllergyTypeAsync(Guid allergyTypeId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM PatientAllergies WHERE AllergyId = :Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = allergyTypeId.ToString() });
        return count > 0;
    }

    public async Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Name, CreatedAtUtc, UpdatedAtUtc FROM AllergyTypes WHERE Id = :Id";
        return await connection.QuerySingleOrDefaultAsync<AllergyType>(sql, new { Id = allergyTypeId.ToString() });
    }

    public async Task<Guid> UpdateAllergyTypeAsync(AllergyType allergyType)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "UPDATE AllergyTypes SET Name = :Name, UpdatedAtUtc = SYSTIMESTAMP WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = allergyType.Id.ToString(), allergyType.Name });
        return allergyType.Id;
    }

    public async Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> SearchAllergyTypesAsync(
        PageQuery query,
        int offset)
    {
        using var connection = connectionFactory.CreateConnection();
        const string where = "WHERE (:Search IS NULL OR UPPER(Name) LIKE '%' || UPPER(:Search) || '%')";
        var parameters = new { Search = query.NormalizedSearch, Offset = offset, query.PageSize };
        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM AllergyTypes {where}",
            parameters);
        var items = (await connection.QueryAsync<AllergyType>(
            $"""
            SELECT Id, Name, CreatedAtUtc, UpdatedAtUtc
            FROM AllergyTypes
            {where}
            ORDER BY Name, Id
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """,
            parameters)).AsList();
        return (items, totalCount);
    }
}