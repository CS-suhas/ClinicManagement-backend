using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using ClinicManagement.Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class ProviderRepositoryIntegrationTests
{
    [Fact]
    public async Task ProviderRepository_PersistsAndUpdatesProviderInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinicRepository = new ClinicRepository(connectionFactory);
        var repository = new ProviderRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101");
        var provider = Provider.Create(providerId, clinicId, "Grace", "Hopper", "General Practice");

        try
        {
            Assert.Equal(clinicId, await clinicRepository.AddClinicAsync(clinic));
            Assert.Equal(providerId, await repository.AddProviderAsync(provider, clinicId));

            var storedProvider = await repository.GetProviderByIdAsync(providerId);
            Assert.NotNull(storedProvider);
            Assert.Equal("Grace", storedProvider.FirstName);
            Assert.Equal("General Practice", storedProvider.Specialty);
            Assert.Equal(clinicId, storedProvider.Clinic.Id);
            Assert.Equal(clinic.Name, storedProvider.Clinic.Name);
            Assert.Contains(
                await repository.GetAllProvidersAsync(),
                result => result.Id == providerId);

            provider.SetSpecialty("Neurology");
            Assert.Equal(providerId, await repository.UpdateProviderAsync(provider, clinicId));

            var updatedProvider = await repository.GetProviderByIdAsync(providerId);
            Assert.NotNull(updatedProvider);
            Assert.Equal("Neurology", updatedProvider.Specialty);

            Assert.Equal(providerId, await repository.DeleteProviderAsync(providerId));
            Assert.Null(await repository.GetProviderByIdAsync(providerId));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM Providers WHERE Id = :Id", new { Id = providerId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = clinicId.ToString() });
        }
    }
}
