using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class ClinicRepositoryIntegrationTests
{
    [Fact]
    public async Task ClinicRepository_PersistsAndUpdatesClinicInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var repository = new ClinicRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101");

        try
        {
            Assert.Equal(clinicId, await repository.AddClinicAsync(clinic));

            var storedClinic = await repository.GetClinicByIdAsync(clinicId);
            Assert.NotNull(storedClinic);
            Assert.Equal(clinic.Name, storedClinic.Name);
            Assert.Equal("1 Test Street", storedClinic.Address);
            Assert.True(await repository.ClinicExistsAsync(clinicId));
            Assert.Contains(
                await repository.GetAllClinicsAsync(),
                result => result.Id == clinicId);

            clinic.SetName($"Updated clinic {clinicId:N}");
            clinic.SetAddress("2 Test Street");
            Assert.Equal(clinicId, await repository.UpdateClinicAsync(clinic));

            var updatedClinic = await repository.GetClinicByIdAsync(clinicId);
            Assert.NotNull(updatedClinic);
            Assert.Equal(clinic.Name, updatedClinic.Name);
            Assert.Equal("2 Test Street", updatedClinic.Address);

            Assert.Equal(clinicId, await repository.DeleteClinicAsync(clinicId));
            Assert.Null(await repository.GetClinicByIdAsync(clinicId));
            Assert.False(await repository.ClinicExistsAsync(clinicId));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = clinicId.ToString() });
        }
    }
}
