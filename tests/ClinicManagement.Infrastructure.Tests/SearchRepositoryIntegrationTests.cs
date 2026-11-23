using System.Data;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class SearchRepositoryIntegrationTests
{
    [Fact]
    public async Task SearchRepository_FindsPatientsByNameAndEmailWithinScopeInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var appointments = new AppointmentRepository(connectionFactory);
        var repository = new SearchRepository(connectionFactory);

        // A unique token keeps the matches to rows seeded here, so exact counts are safe.
        var token = $"sx{Guid.NewGuid():N}"[..14];
        var clinicAId = Guid.NewGuid();
        var clinicBId = Guid.NewGuid();
        var providerAId = Guid.NewGuid();
        var providerBId = Guid.NewGuid();
        var byFirstNameId = Guid.NewGuid();
        var byLastNameId = Guid.NewGuid();
        var byEmailId = Guid.NewGuid();
        var appointmentAId = Guid.NewGuid();
        var appointmentBId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        try
        {
            await clinics.AddClinicAsync(Clinic.Create(clinicAId, $"Clinic A {clinicAId:N}", "1 Test Street", "555-0101"));
            await clinics.AddClinicAsync(Clinic.Create(clinicBId, $"Clinic B {clinicBId:N}", "2 Test Street", "555-0102"));
            await providers.AddProviderAsync(Provider.Create(providerAId, clinicAId, "Grace", "Hopper", "General Practice"), clinicAId);
            await providers.AddProviderAsync(Provider.Create(providerBId, clinicBId, "Alan", "Turing", "Neurology"), clinicBId);

            var emailA = $"ada.{byFirstNameId:N}@example.com";
            var emailB = $"bea.{byLastNameId:N}@example.com";
            var emailC = $"{token}@example.com";
            await patients.AddPatientAsync(Patient.Create(byFirstNameId, $"Ada{token}", "Lovelace", new DateOnly(1985, 12, 10), emailA, "555-0100"));
            await patients.AddPatientAsync(Patient.Create(byLastNameId, "Bea", $"Smith{token}", new DateOnly(1990, 1, 2), emailB, "555-0103"));
            await patients.AddPatientAsync(Patient.Create(byEmailId, "Cy", "Jones", new DateOnly(1975, 5, 6), emailC, "555-0104"));

            await appointments.AddAppointmentAsync(
                Appointment.Create(appointmentAId, byFirstNameId, providerAId, now.AddDays(1), null, now),
                byFirstNameId,
                providerAId);
            await appointments.AddAppointmentAsync(
                Appointment.Create(appointmentBId, byLastNameId, providerBId, now.AddDays(1), null, now),
                byLastNameId,
                providerBId);

            var all = await repository.SearchPatientsAsync(token, 50, PatientSearchScope.All);
            Assert.Equal(3, all.Count);
            var byFirst = Assert.Single(all, result => result.Id == byFirstNameId);
            Assert.Equal($"Ada{token} Lovelace", byFirst.Name);
            Assert.Equal(emailA, byFirst.Subtitle);
            Assert.Contains(all, result => result.Id == byLastNameId);
            Assert.Contains(all, result => result.Id == byEmailId && result.Subtitle == emailC);

            var limited = await repository.SearchPatientsAsync(token, 2, PatientSearchScope.All);
            Assert.Equal(2, limited.Count);

            var byProvider = await repository.SearchPatientsAsync(
                token, 50, new PatientSearchScope(false, providerAId, []));
            Assert.Equal(new[] { byFirstNameId }, byProvider.Select(result => result.Id));

            var byClinic = await repository.SearchPatientsAsync(
                token, 50, new PatientSearchScope(false, null, [clinicBId]));
            Assert.Equal(new[] { byLastNameId }, byClinic.Select(result => result.Id));

            var byProviderOrClinic = await repository.SearchPatientsAsync(
                token, 50, new PatientSearchScope(false, providerAId, [clinicBId]));
            Assert.Equal(
                new[] { byFirstNameId, byLastNameId }.OrderBy(id => id.ToString()).ToArray(),
                byProviderOrClinic.Select(result => result.Id).OrderBy(id => id.ToString()).ToArray());

            var noScope = await repository.SearchPatientsAsync(token, 50, new PatientSearchScope(false, null, []));
            Assert.Empty(noScope);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Appointments WHERE ProviderId IN :Ids", [providerAId, providerBId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Providers WHERE Id IN :Ids", [providerAId, providerBId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Patients WHERE Id IN :Ids", [byFirstNameId, byLastNameId, byEmailId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [clinicAId, clinicBId]);
        }
    }

    [Fact]
    public async Task SearchRepository_FindsProvidersAndHonoursVerifiedOnlyInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var repository = new SearchRepository(connectionFactory);

        var token = $"sx{Guid.NewGuid():N}"[..14];
        var clinicId = Guid.NewGuid();
        var verifiedByNameId = Guid.NewGuid();
        var verifiedBySpecialtyId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();
        var clinicName = $"Clinic {clinicId:N}";

        try
        {
            await clinics.AddClinicAsync(Clinic.Create(clinicId, clinicName, "1 Test Street", "555-0101"));
            await providers.AddProviderAsync(Provider.Create(verifiedByNameId, clinicId, "Grace", token, "General Practice"), clinicId);
            await providers.AddProviderAsync(Provider.Create(verifiedBySpecialtyId, clinicId, "Alan", "Turing", $"Cardiology {token}"), clinicId);
            await providers.AddRegistrationAsync(Provider.Register(pendingId, "Pending", token, "Dermatology"));

            var everyone = await repository.SearchProvidersAsync(token, 50, verifiedOnly: false);
            Assert.Equal(3, everyone.Count);

            var byName = Assert.Single(everyone, result => result.Id == verifiedByNameId);
            Assert.Equal($"Grace {token}", byName.Name);
            Assert.Equal("General Practice", byName.Specialty);
            Assert.Equal(clinicName, byName.ClinicName);

            var bySpecialty = Assert.Single(everyone, result => result.Id == verifiedBySpecialtyId);
            Assert.Equal($"Cardiology {token}", bySpecialty.Specialty);

            var pending = Assert.Single(everyone, result => result.Id == pendingId);
            Assert.Null(pending.ClinicName);

            var verifiedOnly = await repository.SearchProvidersAsync(token, 50, verifiedOnly: true);
            Assert.Equal(
                new[] { verifiedByNameId, verifiedBySpecialtyId }.OrderBy(id => id.ToString()).ToArray(),
                verifiedOnly.Select(result => result.Id).OrderBy(id => id.ToString()).ToArray());

            var limited = await repository.SearchProvidersAsync(token, 1, verifiedOnly: false);
            Assert.Single(limited);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Providers WHERE Id IN :Ids", [verifiedByNameId, verifiedBySpecialtyId, pendingId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [clinicId]);
        }
    }

    [Fact]
    public async Task SearchRepository_FindsClinicsByNameOrAddressInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var repository = new SearchRepository(connectionFactory);

        var token = $"sx{Guid.NewGuid():N}"[..14];
        var byNameId = Guid.NewGuid();
        var byAddressId = Guid.NewGuid();
        var unrelatedId = Guid.NewGuid();
        var address = $"{token} Road";

        try
        {
            await clinics.AddClinicAsync(Clinic.Create(byNameId, $"Clinic {token}", null, "555-0101"));
            await clinics.AddClinicAsync(Clinic.Create(byAddressId, $"Other {byAddressId:N}", address, "555-0102"));
            await clinics.AddClinicAsync(Clinic.Create(unrelatedId, $"Unrelated {unrelatedId:N}", "9 Elsewhere", "555-0103"));

            var results = await repository.SearchClinicsAsync(token, 50);
            Assert.Equal(new[] { byNameId, byAddressId }, results.Select(result => result.Id));
            Assert.Null(results[0].Address);
            Assert.Equal(address, results[1].Address);

            var limited = await repository.SearchClinicsAsync(token, 1);
            Assert.Equal(new[] { byNameId }, limited.Select(result => result.Id));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [byNameId, byAddressId, unrelatedId]);
        }
    }

    private static async Task DeleteWhereInAsync(IDbConnection connection, string sql, Guid[] ids)
    {
        await connection.ExecuteAsync(sql, new { Ids = ids.Select(id => id.ToString()).ToArray() });
    }
}
