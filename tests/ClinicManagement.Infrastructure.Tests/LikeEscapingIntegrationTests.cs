using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using ClinicManagement.Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

/// <summary>%, _ and \ typed into a search box must match literally, not act as LIKE wildcards.</summary>
public class LikeEscapingIntegrationTests
{
    [Fact]
    public async Task Searches_TreatLikeWildcardsInUserTextLiterallyInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var search = new SearchRepository(connectionFactory);
        var token = Guid.NewGuid().ToString("N")[..12];

        // Same prefix and suffix; only the literal one contains "50%_\off".
        var literal = Clinic.Create(Guid.NewGuid(), $"Lk{token} 50%_\\off", "1 Road", "555-0100");
        var lookalike = Clinic.Create(Guid.NewGuid(), $"Lk{token} 50xyz9off", "2 Road", "555-0101");

        try
        {
            await clinics.AddClinicAsync(literal);
            await clinics.AddClinicAsync(lookalike);

            var quick = await search.SearchClinicsAsync($"Lk{token} 50%_\\off", 10);
            Assert.Equal([literal.Id], quick.Select(result => result.Id));

            var (paged, total) = await clinics.SearchClinicsAsync(new PageQuery($"Lk{token} 50%"), 0);
            Assert.Equal(1, total);
            Assert.Equal([literal.Id], paged.Select(clinic => clinic.Id));

            // An underscore alone must not match any single character.
            Assert.Empty(await search.SearchClinicsAsync($"Lk{token} 50_", 10));

            // A plain term still matches both.
            Assert.Equal(2, (await search.SearchClinicsAsync($"Lk{token}", 10)).Count);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "DELETE FROM Clinics WHERE Id IN (:A, :B)",
                new { A = literal.Id.ToString(), B = lookalike.Id.ToString() });
        }
    }
}
