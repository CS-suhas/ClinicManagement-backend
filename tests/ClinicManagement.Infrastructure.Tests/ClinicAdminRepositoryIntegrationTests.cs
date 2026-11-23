using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class ClinicAdminRepositoryIntegrationTests
{
    [Fact]
    public async Task ClinicAdminRepository_AddsAndResolvesClinicAdminsInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var users = new UserRepository(connectionFactory);
        var clinics = new ClinicRepository(connectionFactory);
        var repository = new ClinicAdminRepository(connectionFactory);
        var adminSubject = $"it-{Guid.NewGuid():N}";
        var otherSubject = $"it-{Guid.NewGuid():N}";
        var emptySubject = $"it-{Guid.NewGuid():N}";
        var firstClinicId = Guid.NewGuid();
        var secondClinicId = Guid.NewGuid();
        var unassignedClinicId = Guid.NewGuid();

        try
        {
            var admin = await users.GetOrCreateAsync(adminSubject, null, "Admin");
            var other = await users.GetOrCreateAsync(otherSubject, null, "Other");
            var empty = await users.GetOrCreateAsync(emptySubject, null, "Empty");

            await clinics.AddClinicAsync(Clinic.Create(firstClinicId, $"Clinic {firstClinicId:N}", "1 Test Street", "555-0101"));
            await clinics.AddClinicAsync(Clinic.Create(secondClinicId, $"Clinic {secondClinicId:N}", "2 Test Street", "555-0102"));
            await clinics.AddClinicAsync(Clinic.Create(unassignedClinicId, $"Clinic {unassignedClinicId:N}", "3 Test Street", "555-0103"));

            await repository.AddAsync(ClinicAdmin.Create(Guid.NewGuid(), firstClinicId, admin.Id));
            await repository.AddAsync(ClinicAdmin.Create(Guid.NewGuid(), secondClinicId, admin.Id));
            await repository.AddAsync(ClinicAdmin.Create(Guid.NewGuid(), firstClinicId, other.Id));

            Assert.True(await repository.IsAdminOfClinicAsync(admin.Id, firstClinicId));
            Assert.True(await repository.IsAdminOfClinicAsync(admin.Id, secondClinicId));
            Assert.False(await repository.IsAdminOfClinicAsync(admin.Id, unassignedClinicId));
            Assert.True(await repository.IsAdminOfClinicAsync(other.Id, firstClinicId));
            Assert.False(await repository.IsAdminOfClinicAsync(other.Id, secondClinicId));
            Assert.False(await repository.IsAdminOfClinicAsync(empty.Id, firstClinicId));

            var expectedClinicIds = new[] { firstClinicId, secondClinicId }
                .OrderBy(id => id.ToString(), StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(expectedClinicIds, await repository.GetClinicIdsAsync(admin.Id));
            Assert.Equal(new[] { firstClinicId }, await repository.GetClinicIdsAsync(other.Id));
            Assert.Empty(await repository.GetClinicIdsAsync(empty.Id));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "DELETE FROM ClinicAdmins WHERE ClinicId IN :ClinicIds",
                new
                {
                    ClinicIds = new[] { firstClinicId.ToString(), secondClinicId.ToString(), unassignedClinicId.ToString() }
                });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = firstClinicId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = secondClinicId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = unassignedClinicId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Users WHERE IdentitySubject = :Subject", new { Subject = adminSubject });
            await connection.ExecuteAsync("DELETE FROM Users WHERE IdentitySubject = :Subject", new { Subject = otherSubject });
            await connection.ExecuteAsync("DELETE FROM Users WHERE IdentitySubject = :Subject", new { Subject = emptySubject });
        }
    }
}
