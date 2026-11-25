using System.Net;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;

namespace ClinicManagement.Api.Tests;

public sealed class UserRoleEndpointTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    private const string Admin = ClinicApiFactory.PlatformAdminSubject;

    [Fact]
    public async Task Routes_RequireSignIn()
    {
        var target = Subject("target");

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/user-roles", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Put, $"/api/users/{target}/roles/admin", null)).StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task Routes_AreForbiddenToNonAdministrators(string role)
    {
        var world = SeedWorld();
        var caller = world.SubjectFor(role);
        var target = Subject("target");

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/user-roles", caller)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"/api/users/{target}/roles", caller)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Put, $"/api/users/{target}/roles/admin", caller)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Delete, $"/api/users/{target}/roles/admin", caller)).StatusCode);
    }

    [Fact]
    public async Task AssignedAdmin_GainsAdminAccess_AndLosesItWhenRevoked()
    {
        var target = Subject("target");
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/dashboard", target)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Put, $"/api/users/{target}/roles/admin", Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Put, $"/api/users/{target}/roles/admin", Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/dashboard", target)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"/api/users/{target}/roles/admin", Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"/api/users/{target}/roles/admin", Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/dashboard", target)).StatusCode);
    }

    [Fact]
    public async Task GetUserRoles_ReturnsAssignedAndEffectiveRoles()
    {
        var world = SeedWorld();
        await SendAsync(HttpMethod.Put, $"/api/users/{world.PatientSubject}/roles/admin", Admin);

        var response = await SendAsync(HttpMethod.Get, $"/api/users/{world.PatientSubject}/roles", Admin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(["admin"], body.GetProperty("assignedRoles").EnumerateArray().Select(role => role.GetString()));
        Assert.Equal(["patient", "admin"], body.GetProperty("effectiveRoles").EnumerateArray().Select(role => role.GetString()));
    }

    [Fact]
    public async Task ListUserRoles_IncludesWhoGrantedTheRole()
    {
        var target = Subject("target");
        await SendAsync(HttpMethod.Put, $"/api/users/{target}/roles/admin", Admin);

        var response = await SendAsync(HttpMethod.Get, "/api/user-roles", Admin);

        var assignment = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement
            .EnumerateArray()
            .Single(item => item.GetProperty("identitySubject").GetString() == target);
        Assert.Equal("admin", assignment.GetProperty("role").GetString());
        Assert.Equal(
            Factory.Store.UserFor(Admin).Id,
            assignment.GetProperty("grantedByUserId").GetGuid());
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    [InlineData("superuser")]
    public async Task AssigningNonAssignableRole_IsRejected(string role)
    {
        var response = await SendAsync(HttpMethod.Put, $"/api/users/{Subject("target")}/roles/{role}", Admin);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RevokingOwnAdminRole_IsAConflict()
    {
        var otherAdmin = Subject("admin");
        await SendAsync(HttpMethod.Put, $"/api/users/{otherAdmin}/roles/admin", Admin);

        var response = await SendAsync(HttpMethod.Delete, $"/api/users/{otherAdmin}/roles/admin", otherAdmin);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RevokingConfiguredAdmin_IsAConflict()
    {
        var otherAdmin = Subject("admin");
        await SendAsync(HttpMethod.Put, $"/api/users/{otherAdmin}/roles/admin", Admin);

        var response = await SendAsync(HttpMethod.Delete, $"/api/users/{Admin}/roles/admin", otherAdmin);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UnknownUser_IsNotFound()
    {
        var unknown = Subject("nobody");

        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/users/{unknown}/roles", Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Delete, $"/api/users/{unknown}/roles/admin", Admin)).StatusCode);
    }
}
