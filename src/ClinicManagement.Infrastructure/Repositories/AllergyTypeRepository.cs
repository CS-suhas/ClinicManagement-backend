using ClinicManagement.Domain.Entities;
using Infrastructure.Data;
using Dapper;
using ClinicManagement.Application.Allergies.Interfaces;

namespace ClinicManagement.Infrastructure.Repositories;

public class AllergyTypeRepository(IDbConnectionFactory connectionFactory) : IAllergyTypeRepository
{
    public async Task<Guid> AddAllergyTypeAsync(AllergyType allergyType)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "INSERT INTO AllergyTypes (Id, Name) VALUES (@Id, @Name)";
        await connection.ExecuteAsync(sql, new { allergyType.Id, allergyType.Name });
        return allergyType.Id;
    }

    public async Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM AllergyTypes WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = allergyTypeId });
        return allergyTypeId;
    }

    public async Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM AllergyTypes";
        return await connection.QueryAsync<AllergyType>(sql);
    }

    public async Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM AllergyTypes WHERE Id = @Id";
        return await connection.QuerySingleOrDefaultAsync<AllergyType>(sql, new { Id = allergyTypeId });
    }

    public async Task<Guid> UpdateAllergyTypeAsync(AllergyType allergyType)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "UPDATE AllergyTypes SET Name = @Name WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { allergyType.Id, allergyType.Name });
        return allergyType.Id;
    }
}