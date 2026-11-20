using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Domain.Entities;
using Dapper;
using Infrastructure.Data;

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
        const string sql = "INSERT INTO Patients (Id, FirstName, LastName, DateOfBirth, PhoneNumber, Email) VALUES (:Id, :FirstName, :LastName, :DateOfBirth, :PhoneNumber, :Email)";
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
}