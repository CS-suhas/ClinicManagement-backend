using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using ClinicManagement.Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class PatientRepositoryIntegrationTests
{
    [Fact]
    public async Task PatientRepository_PersistsAndUpdatesPatientInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var repository = new PatientRepository(connectionFactory);
        var patientId = Guid.NewGuid();
        var patient = Patient.Create(
            patientId,
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            $"ada.{patientId:N}@example.com",
            "555-0100");

        try
        {
            Assert.Equal(patientId, await repository.AddPatientAsync(patient));

            var storedPatient = await repository.GetPatientByIdAsync(patientId);
            Assert.NotNull(storedPatient);
            Assert.Equal("Ada", storedPatient.FirstName);
            Assert.Equal(new DateOnly(1985, 12, 10), storedPatient.DateOfBirth);
            Assert.True(await repository.PatientExistsAsync(patientId));
            Assert.Contains(
                await repository.GetAllPatientsAsync(),
                result => result.Id == patientId);

            patient.SetFirstName("Augusta");
            Assert.Equal(patientId, await repository.UpdatePatientAsync(patient));

            var updatedPatient = await repository.GetPatientByIdAsync(patientId);
            Assert.NotNull(updatedPatient);
            Assert.Equal("Augusta", updatedPatient.FirstName);

            Assert.Equal(patientId, await repository.DeletePatientAsync(patientId));
            Assert.Null(await repository.GetPatientByIdAsync(patientId));
            Assert.False(await repository.PatientExistsAsync(patientId));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM Patients WHERE Id = :Id", new { Id = patientId.ToString() });
        }
    }
}
