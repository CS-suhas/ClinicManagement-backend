using System.Security.Claims;
using ClinicManagement.Api.Security;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Domain.Entities;
using Microsoft.Extensions.Configuration;

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
    public async Task PlatformAdminUser_DerivesAdminRole()
    {
        store.UserFor(Subject).GrantPlatformAdmin();

        var roles = await RolesFor(Subject);

        Assert.Equal(["admin"], roles);
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

        await transformation.TransformAsync(principal);
        await transformation.TransformAsync(principal);

        Assert.Single(principal.FindAll(ClaimTypes.Role));
        Assert.Single(principal.FindAll(ClinicClaimTypes.UserId));
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

    private RoleClaimsTransformation CreateTransformation(IEnumerable<string> configuredAdmins)
    {
        var values = new Dictionary<string, string?>();
        var index = 0;
        foreach (var admin in configuredAdmins)
            values[$"Authorization:AdminSubjects:{index++}"] = admin;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        return new RoleClaimsTransformation(configuration, new InMemoryUserRepository(store));
    }

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
}
