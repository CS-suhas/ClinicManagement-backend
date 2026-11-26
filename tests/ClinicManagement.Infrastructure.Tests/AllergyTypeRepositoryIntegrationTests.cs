using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using ClinicManagement.Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class AllergyTypeRepositoryIntegrationTests
{
    [Fact]
    public async Task AllergyTypeRepository_PersistsAndUpdatesAllergyTypeInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var repository = new AllergyTypeRepository(connectionFactory);
        var allergyTypeId = Guid.NewGuid();
        var allergyType = AllergyType.Create(allergyTypeId, $"Allergy {allergyTypeId:N}");

        try
        {
            Assert.Equal(allergyTypeId, await repository.AddAllergyTypeAsync(allergyType));

            var storedAllergyType = await repository.GetAllergyTypeByIdAsync(allergyTypeId);
            Assert.NotNull(storedAllergyType);
            Assert.Equal(allergyType.Name, storedAllergyType.Name);
            Assert.Contains(
                await repository.GetAllAllergyTypesAsync(),
                result => result.Id == allergyTypeId);

            allergyType.SetName($"Updated allergy {allergyTypeId:N}");
            Assert.Equal(allergyTypeId, await repository.UpdateAllergyTypeAsync(allergyType));

            var updatedAllergyType = await repository.GetAllergyTypeByIdAsync(allergyTypeId);
            Assert.NotNull(updatedAllergyType);
            Assert.Equal(allergyType.Name, updatedAllergyType.Name);

            Assert.Equal(allergyTypeId, await repository.DeleteAllergyTypeAsync(allergyTypeId));
            Assert.Null(await repository.GetAllergyTypeByIdAsync(allergyTypeId));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM AllergyTypes WHERE Id = :Id", new { Id = allergyTypeId.ToString() });
        }
    }
}
