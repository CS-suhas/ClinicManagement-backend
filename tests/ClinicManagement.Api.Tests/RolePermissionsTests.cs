using System.Security.Claims;
using ClinicManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagement.Api.Tests;

public class RolePermissionsTests
{
    [Fact]
    public void EveryPermission_IsHeldByAtLeastOneRole()
    {
        var granted = RolePermissions.For(["patient", "doctor", "clinicadmin", "admin"]);

        Assert.Equal(Permissions.All.Order(StringComparer.Ordinal), granted);
    }

    [Fact]
    public void JoinRequestReview_IsHeldByClinicAdminAndAdmin()
    {
        Assert.True(RolePermissions.Grants("clinicadmin", Permissions.JoinRequestsReview));
        Assert.True(RolePermissions.Grants("admin", Permissions.JoinRequestsReview));
        Assert.False(RolePermissions.Grants("doctor", Permissions.JoinRequestsReview));
    }

    [Fact]
    public void Admin_DoesNotHoldPatientOrDoctorActions()
    {
        Assert.False(RolePermissions.Grants("admin", Permissions.AppointmentsBook));
        Assert.False(RolePermissions.Grants("admin", Permissions.AppointmentsRespond));
        Assert.False(RolePermissions.Grants("admin", Permissions.OwnPatientProfileEdit));
    }

    [Fact]
    public void UnknownRole_HoldsNothing()
    {
        Assert.Empty(RolePermissions.For(["receptionist"]));
        Assert.False(RolePermissions.Grants("receptionist", Permissions.ClinicsManage));
    }

    [Theory]
    [InlineData("admin", true)]
    [InlineData("ADMIN", true)]
    [InlineData("doctor", false)]
    public async Task Handler_SucceedsOnlyWhenARoleHoldsThePermission(string role, bool expected)
    {
        var requirement = new PermissionRequirement(Permissions.ClinicsManage);
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.Equal(expected, context.HasSucceeded);
    }
}
