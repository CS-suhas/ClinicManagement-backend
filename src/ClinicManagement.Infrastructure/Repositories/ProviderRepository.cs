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
        const string sql = """
            SELECT pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId,
                   c.Id, c.Name, c.Address, c.PhoneNumber
            FROM Providers pr
            INNER JOIN Clinics c ON c.Id = pr.ClinicId
            """;
        return await connection.QueryAsync<Provider, Clinic, Provider>(
            sql,
            (provider, clinic) =>
            {
                provider.SetClinic(clinic);
                return provider;
            },
            splitOn: "Id");
    }

    public async Task<Guid> UpdateProviderAsync(Provider provider, Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "UPDATE Providers SET FirstName = :FirstName, LastName = :LastName, Specialty = :Specialty, ClinicId = :ClinicId, UpdatedAtUtc = SYSTIMESTAMP WHERE Id = :Id";
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
        const string sql = """
            SELECT pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId,
                   c.Id, c.Name, c.Address, c.PhoneNumber
            FROM Providers pr
            INNER JOIN Clinics c ON c.Id = pr.ClinicId
            WHERE pr.Id = :Id
            """;
        var providers = await connection.QueryAsync<Provider, Clinic, Provider>(
            sql,
            (provider, clinic) =>
            {
                provider.SetClinic(clinic);
                return provider;
            },
            new { Id = providerId.ToString() },
            splitOn: "Id");
        return providers.SingleOrDefault();
    }

    
}