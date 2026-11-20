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
}