using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Domain.Entities;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Repositories;

public class ClinicRepository(IDbConnectionFactory connectionFactory) : IClinicRepository
{
    public async Task<Clinic?> GetClinicByIdAsync(Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Name, Address, PhoneNumber, CreatedAtUtc, UpdatedAtUtc FROM Clinics WHERE Id = :Id";
        return await connection.QuerySingleOrDefaultAsync<Clinic>(sql, new { Id = clinicId.ToString() });
    }

    public async Task<IEnumerable<Clinic>> GetAllClinicsAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT Id, Name, Address, PhoneNumber, CreatedAtUtc, UpdatedAtUtc FROM Clinics";
        return await connection.QueryAsync<Clinic>(sql);
    }

    public async Task<Guid> AddClinicAsync(Clinic clinic)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "INSERT INTO Clinics (Id, Name, Address, PhoneNumber) VALUES (:Id, :Name, :Address, :PhoneNumber)";
        await connection.ExecuteAsync(sql, new
        {
            Id = clinic.Id.ToString(),
            clinic.Name,
            clinic.Address,
            clinic.PhoneNumber
        });
        return clinic.Id;
    }

    public async Task<Guid> UpdateClinicAsync(Clinic clinic)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "UPDATE Clinics SET Name = :Name, Address = :Address, PhoneNumber = :PhoneNumber, UpdatedAtUtc = SYSTIMESTAMP WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new
        {
            Id = clinic.Id.ToString(),
            clinic.Name,
            clinic.Address,
            clinic.PhoneNumber
        });
        return clinic.Id;
    }

    public async Task<Guid> DeleteClinicAsync(Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM Clinics WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = clinicId.ToString() });
        return clinicId;
    }

    public async Task<bool> ClinicExistsAsync(Guid clinicId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Clinics WHERE Id = :Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = clinicId.ToString() });
        return count > 0;
    }
}