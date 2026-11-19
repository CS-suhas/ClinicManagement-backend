using ClinicManagement.Domain.Entities;
using ClinicManagement.Application.Providers.Interfaces;
using Infrastructure.Data;
using Dapper;

namespace ClinicManagement.Infrastructure.Repositories;

public class ProviderRepository(IDbConnectionFactory connectionFactory) : IProviderRepository
{
    public async Task<Guid> AddProviderAsync(Provider provider, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "INSERT INTO Providers (Id, FirstName, LastName, Specialty, ClinicId) VALUES (:Id, :FirstName, :LastName, :Specialty, :ClinicId)";
        await connection.ExecuteAsync(sql, new
        {
            Id = provider.Id.ToString(),
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            ClinicId = clinicId.ToString()
        });
        return provider.Id;
    }

    public async Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM Providers WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = providerId.ToString() });
        return providerId;
    }

    public async Task<IEnumerable<Provider>> GetAllProvidersAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM Providers";
        return await connection.QueryAsync<Provider>(sql);
    }

    public async Task<Guid> UpdateProviderAsync(Provider provider, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "UPDATE Providers SET FirstName = :FirstName, LastName = :LastName, Specialty = :Specialty, ClinicId = :ClinicId WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new
        {
            Id = provider.Id.ToString(),
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            ClinicId = clinicId.ToString()
        });
        return provider.Id;
    }

    public async Task<Provider?> GetProviderByIdAsync(Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT * FROM Providers WHERE Id = :Id";
        return await connection.QuerySingleOrDefaultAsync<Provider>(sql, new { Id = providerId.ToString() });
    }

    
}