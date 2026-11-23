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
        provider.AssignIdentitySubject(Subject);
        provider.Reject();
        store.Providers.Add(provider);

        var roles = await RolesFor(Subject);

        Assert.Empty(roles);
    }

    [Fact]
    public async Task ClinicAdminRow_DerivesClinicAdminRole()
    {
        store.ClinicAdmins.Add(ClinicAdmin.Create(Guid.NewGuid(), Guid.NewGuid(), Subject));

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
    public async Task PlatformAdminRow_DerivesAdminRole()
    {
        store.PlatformAdminSubjects.Add(Subject);

        var roles = await RolesFor(Subject);

        Assert.Equal(["admin"], roles);
    }

    [Fact]
    public async Task IdentityWithSeveralRecords_DerivesEveryMatchingRole()
    {
        AddPatient(Subject);
        AddProvider(Subject, verified: true);
        store.ClinicAdmins.Add(ClinicAdmin.Create(Guid.NewGuid(), Guid.NewGuid(), Subject));

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

        return new RoleClaimsTransformation(
            configuration,
            new InMemoryPatientRepository(store),
            new InMemoryProviderRepository(store),
            new InMemoryClinicAdminRepository(store),
            new InMemoryPlatformAdminRepository(store));
    }

    private void AddPatient(string subject)
    {
        var patient = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        patient.AssignIdentitySubject(subject);
        store.Patients.Add(patient);
    }

    private void AddProvider(string subject, bool verified)
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        provider.AssignIdentitySubject(subject);
        if (verified)
            provider.Verify();
        store.Providers.Add(provider);
    }
}
