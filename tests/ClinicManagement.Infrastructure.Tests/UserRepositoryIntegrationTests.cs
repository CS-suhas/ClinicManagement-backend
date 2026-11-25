using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class UserRepositoryIntegrationTests
{
    [Fact]
    public async Task UserRepository_CreatesUsersLinksProfilesAndManagesRolesAndResolvesAccessContextInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var users = new UserRepository(connectionFactory);
        var userRoles = new UserRoleRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var clinics = new ClinicRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var subject = $"it-{Guid.NewGuid():N}";
        Guid? userId = null;
        var patientId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        try
        {
            var created = await users.GetOrCreateAsync(subject, "ada@example.com", "Ada");
            userId = created.Id;

            var again = await users.GetOrCreateAsync(subject, null, "Ada King");
            Assert.Equal(created.Id, again.Id);

            var stored = await users.GetBySubjectAsync(subject);
            Assert.NotNull(stored);
            Assert.Equal("ada@example.com", stored.Email);
            Assert.Equal("Ada King", stored.DisplayName);

            var empty = await users.GetAccessContextAsync(created.Id);
            Assert.Empty(empty.AssignedRoles);
            Assert.Null(empty.PatientId);
            Assert.Null(empty.ProviderId);
            Assert.Empty(empty.AdministeredClinicIds);

            var patient = Patient.Create(patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), $"ada.{patientId:N}@example.com", "555-0100");
            patient.AssignUser(created.Id);
            await patients.AddPatientAsync(patient);

            await clinics.AddClinicAsync(Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101"));
            var provider = Provider.Register(providerId, clinicId, "Ada", "Lovelace", "Computing");
            provider.AssignUser(created.Id);
            await providers.AddProviderAsync(provider, clinicId);

            Assert.Equal(patientId, (await patients.GetByUserIdAsync(created.Id))?.Id);
            Assert.Equal(providerId, (await providers.GetByUserIdAsync(created.Id))?.Id);

            Assert.True(await userRoles.AssignAsync(created.Id, "admin", grantedByUserId: null));
            Assert.False(await userRoles.AssignAsync(created.Id, "admin", grantedByUserId: null));
            Assert.True(await userRoles.AssignAsync(created.Id, "auditor", grantedByUserId: created.Id));

            var context = await users.GetAccessContextAsync(created.Id);
            Assert.Equal(patientId, context.PatientId);
            Assert.Equal(providerId, context.ProviderId);
            Assert.Equal(ProviderVerificationStatus.PENDING, context.ProviderStatus);
            Assert.Empty(context.AdministeredClinicIds);
            Assert.Equal(["admin", "auditor"], context.AssignedRoles);

            var assignments = (await userRoles.ListAsync()).Where(assignment => assignment.UserId == created.Id).ToList();
            Assert.Equal(["admin", "auditor"], assignments.Select(assignment => assignment.Role));
            Assert.All(assignments, assignment => Assert.Equal(subject, assignment.IdentitySubject));
            Assert.Null(assignments[0].GrantedByUserId);
            Assert.Equal(created.Id, assignments[1].GrantedByUserId);

            Assert.True(await userRoles.RevokeAsync(created.Id, "auditor"));
            Assert.False(await userRoles.RevokeAsync(created.Id, "auditor"));
            Assert.Equal(["admin"], (await users.GetAccessContextAsync(created.Id)).AssignedRoles);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM Patients WHERE Id = :Id", new { Id = patientId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Providers WHERE Id = :Id", new { Id = providerId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = clinicId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Users WHERE IdentitySubject = :Subject", new { Subject = subject });
        }
    }
}
