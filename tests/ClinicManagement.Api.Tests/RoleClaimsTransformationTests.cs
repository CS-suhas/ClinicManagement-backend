using System.Security.Claims;
using ClinicManagement.Api.Security;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Application.Common.Settings;

namespace ClinicManagement.Api.Tests;

public class RoleClaimsTransformationTests
{
    private const string Subject = "kc-subject-1";

    private readonly InMemoryStore store = new();

    [Fact]
    public async Task PatientRow_DerivesPatientRole()
    {
        AddPatient(Subject);

        var roles = await RolesFor(Subject);

        Assert.Equal(["patient"], roles);
    }

    [Fact]
    public async Task VerifiedProvider_DerivesDoctorRole()
    {
        AddProvider(Subject, verified: true);

        var roles = await RolesFor(Subject);

        Assert.Equal(["doctor"], roles);
    }

    [Fact]
    public async Task PendingProvider_DoesNotDeriveDoctorRole()
    {
        AddProvider(Subject, verified: false);

        var roles = await RolesFor(Subject);

        Assert.Empty(roles);
    }

    [Fact]
    public async Task RejectedProvider_DoesNotDeriveDoctorRole()
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        provider.AssignUser(store.UserFor(Subject).Id);
        provider.Reject();
        store.Providers.Add(provider);

        var roles = await RolesFor(Subject);

        Assert.Empty(roles);
    }

    [Fact]
    public async Task ClinicAdminRow_DerivesClinicAdminRole()
    {
        store.ClinicAdmins.Add(ClinicAdmin.Create(Guid.NewGuid(), Guid.NewGuid(), store.UserFor(Subject).Id));

        var roles = await RolesFor(Subject);

        Assert.Equal(["clinicadmin"], roles);
    }

    [Fact]
    public async Task ConfiguredAdminSubject_DerivesAdminRole()
    {
        var roles = await RolesFor(Subject, configuredAdmins: [Subject]);

        Assert.Equal(["admin"], roles);
    }

    [Fact]
    public async Task ConfiguredAdminSubject_IsPersistedAsPlatformAdmin()
    {
        await RolesFor(Subject, configuredAdmins: [Subject]);

        var stored = Assert.Single(store.UserRoles);
        Assert.Equal((store.UserFor(Subject).Id, "admin", (Guid?)null), (stored.UserId, stored.Role, stored.GrantedByUserId));
        Assert.Equal(["admin"], await RolesFor(Subject));
    }

    [Fact]
    public async Task AssignedAdminRole_DerivesAdminRole()
    {
        store.UserRoles.Add(new StoredUserRole(store.UserFor(Subject).Id, "admin", DateTimeOffset.UtcNow, null));

        var roles = await RolesFor(Subject);

        Assert.Equal(["admin"], roles);
    }

    [Fact]
    public async Task AssignedAndDerivedRoles_AreCombined()
    {
        AddPatient(Subject);
        store.UserRoles.Add(new StoredUserRole(store.UserFor(Subject).Id, "admin", DateTimeOffset.UtcNow, null));

        var roles = await RolesFor(Subject);

        Assert.Equal(["patient", "admin"], roles);
    }

    [Fact]
    public async Task IdentityWithSeveralRecords_DerivesEveryMatchingRole()
    {
        AddPatient(Subject);
        AddProvider(Subject, verified: true);
        store.ClinicAdmins.Add(ClinicAdmin.Create(Guid.NewGuid(), Guid.NewGuid(), store.UserFor(Subject).Id));

        var roles = await RolesFor(Subject);

        Assert.Equal(["patient", "doctor", "clinicadmin"], roles);
    }

    [Fact]
    public async Task UnknownSubject_GetsNoRoles()
    {
        AddPatient("someone-else");

        var roles = await RolesFor(Subject);

        Assert.Empty(roles);
    }

    [Fact]
    public async Task InboundRoleClaims_AreReplacedByDerivedRoles()
    {
        AddPatient(Subject);

        var roles = await RolesFor(Subject, inboundRoles: ["admin", "doctor"]);

        Assert.Equal(["patient"], roles);
    }

    [Fact]
    public async Task UnauthenticatedPrincipal_IsReturnedWithoutRoles()
    {
        var transformation = CreateTransformation([]);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await transformation.TransformAsync(principal);

        Assert.Empty(result.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task FirstSignIn_CreatesUserFromTokenAndAddsUserIdClaim()
    {
        var principal = await TransformAsync(
            Subject,
            new Claim("email", "ada@example.com"),
            new Claim("name", "Ada Lovelace"));

        var user = Assert.Single(store.Users);
        Assert.Equal(Subject, user.IdentitySubject);
        Assert.Equal("ada@example.com", user.Email);
        Assert.Equal("Ada Lovelace", user.DisplayName);
        Assert.Equal(user.Id.ToString(), principal.FindFirst(ClinicClaimTypes.UserId)?.Value);
    }

    [Fact]
    public async Task LaterSignIn_ReusesUserAndRefreshesProfile()
    {
        var existing = store.UserFor(Subject);

        await TransformAsync(Subject, new Claim("name", "Ada"));
        var principal = await TransformAsync(Subject, new Claim("name", "Ada King"));

        Assert.Single(store.Users);
        Assert.Equal("Ada King", existing.DisplayName);
        Assert.Single(principal.FindAll(ClinicClaimTypes.UserId));
    }

    [Fact]
    public async Task RunningTwice_DoesNotDuplicateClaims()
    {
        AddPatient(Subject);
        var transformation = CreateTransformation([]);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Subject)], "test"));

        var once = await transformation.TransformAsync(principal);
        var twice = await transformation.TransformAsync(once);

        Assert.Single(twice.FindAll(ClaimTypes.Role));
        Assert.Single(twice.FindAll(ClinicClaimTypes.UserId));
        Assert.Empty(principal.FindAll(ClaimTypes.Role));
    }

    [Fact]
    public async Task SecondRunInSameRequest_ReusesLoadedAccessContext()
    {
        AddPatient(Subject);
        var repository = new CountingUserRepository(new InMemoryUserRepository(store));
        var transformation = new RoleClaimsTransformation(
            new AuthorizationSettings(), repository, new InMemoryUserRoleRepository(store), new UserAccessContextHolder());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Subject)], "test"));

        await transformation.TransformAsync(principal);
        var second = await transformation.TransformAsync(principal);

        Assert.Equal(1, repository.AccessContextLoads);
        Assert.Equal(["patient"], second.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
    }

    private async Task<ClaimsPrincipal> TransformAsync(string subject, params Claim[] extraClaims)
    {
        var identity = new ClaimsIdentity([new Claim("sub", subject), .. extraClaims], "test");
        return await CreateTransformation([]).TransformAsync(new ClaimsPrincipal(identity));
    }

    private async Task<List<string>> RolesFor(
        string subject,
        IEnumerable<string>? configuredAdmins = null,
        IEnumerable<string>? inboundRoles = null)
    {
        var transformation = CreateTransformation(configuredAdmins ?? []);
        var identity = new ClaimsIdentity(
            [new Claim("sub", subject), .. (inboundRoles ?? []).Select(role => new Claim(ClaimTypes.Role, role))],
            "test");

        var result = await transformation.TransformAsync(new ClaimsPrincipal(identity));

        return result.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList();
    }

    private RoleClaimsTransformation CreateTransformation(IEnumerable<string> configuredAdmins) =>
        new(
            new AuthorizationSettings { AdminSubjects = configuredAdmins.ToArray() },
            new InMemoryUserRepository(store),
            new InMemoryUserRoleRepository(store),
            new UserAccessContextHolder());

    private void AddPatient(string subject)
    {
        var patient = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        patient.AssignUser(store.UserFor(subject).Id);
        store.Patients.Add(patient);
    }

    private void AddProvider(string subject, bool verified)
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        provider.AssignUser(store.UserFor(subject).Id);
        if (verified)
            provider.Verify();
        store.Providers.Add(provider);
    }

    private sealed class CountingUserRepository(IUserRepository inner) : IUserRepository
    {
        public int AccessContextLoads { get; private set; }

        public Task<User?> GetBySubjectAsync(string identitySubject) => inner.GetBySubjectAsync(identitySubject);

        public Task<User> GetOrCreateAsync(string identitySubject, string? email = null, string? displayName = null) =>
            inner.GetOrCreateAsync(identitySubject, email, displayName);

        public Task<UserAccessContext> GetAccessContextAsync(Guid userId)
        {
            AccessContextLoads++;
            return inner.GetAccessContextAsync(userId);
        }
    }
}
