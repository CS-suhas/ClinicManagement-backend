using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Tests;

public class CurrentUserGuardTests
{
    [Fact]
    public void RequireAuthenticated_RejectsAnonymousUser()
    {
        var anonymous = new TestCurrentUser { IsAuthenticated = false, Subject = null };

        Assert.Throws<UnauthorizedAccessException>(() => CurrentUserGuard.RequireAuthenticated(anonymous));
    }

    [Fact]
    public void RequireAuthenticated_RejectsUserWithoutSubject()
    {
        var noSubject = new TestCurrentUser { Subject = " " };

        Assert.Throws<UnauthorizedAccessException>(() => CurrentUserGuard.RequireAuthenticated(noSubject));
    }

    [Fact]
    public void RequireSubject_ReturnsSubjectOfSignedInUser()
    {
        var subject = CurrentUserGuard.RequireSubject(new TestCurrentUser { Subject = "kc-1" });

        Assert.Equal("kc-1", subject);
    }

    [Fact]
    public void IsAdministrator_IsTrueOnlyForAdminRole()
    {
        Assert.True(CurrentUserGuard.IsAdministrator(new TestCurrentUser { Roles = ["admin"] }));
        Assert.False(CurrentUserGuard.IsAdministrator(new TestCurrentUser { Roles = ["clinicadmin"] }));
    }

    [Fact]
    public async Task RequireProviderAsync_ForbidsUserWithoutProviderProfile()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => CurrentUserGuard.RequireProviderAsync(new TestCurrentUser(), Guid.NewGuid()));
    }

    [Fact]
    public async Task RequireProviderAsync_ForbidsProviderOtherThanRequested()
    {
        var self = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CurrentUserGuard.RequireProviderAsync(new TestCurrentUser { Provider = self }, Guid.NewGuid()));
    }

    [Fact]
    public async Task RequireProviderOrAdministratorAsync_AllowsAdministratorForAnyProvider()
    {
        await CurrentUserGuard.RequireProviderOrAdministratorAsync(
            new TestCurrentUser { Roles = ["admin"] },
            Guid.NewGuid());
    }

    [Fact]
    public async Task RequireVerifiedProviderAsync_ForbidsUnverifiedProvider()
    {
        var pending = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CurrentUserGuard.RequireVerifiedProviderAsync(new TestCurrentUser { Provider = pending }, pending.Id));
    }

    [Fact]
    public async Task RequireVerifiedProviderAsync_AllowsVerifiedSelf()
    {
        var verified = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        verified.Verify();

        var result = await CurrentUserGuard.RequireVerifiedProviderAsync(
            new TestCurrentUser { Provider = verified },
            verified.Id);

        Assert.Same(verified, result);
    }

    [Fact]
    public async Task RequireClinicAdminAsync_ForbidsAdminOfAnotherClinic()
    {
        var currentUser = new TestCurrentUser { AdministeredClinicIds = [Guid.NewGuid()] };

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CurrentUserGuard.RequireClinicAdminAsync(currentUser, Guid.NewGuid()));
    }

    [Fact]
    public async Task RequireClinicAdminAsync_AllowsAdminOfThatClinic()
    {
        var clinicId = Guid.NewGuid();

        await CurrentUserGuard.RequireClinicAdminAsync(
            new TestCurrentUser { AdministeredClinicIds = [clinicId] },
            clinicId);
    }

    [Fact]
    public async Task RequireClinicAdminOrAdministratorAsync_AllowsAdministratorWithoutClinicAssignment()
    {
        await CurrentUserGuard.RequireClinicAdminOrAdministratorAsync(
            new TestCurrentUser { Roles = ["admin"] },
            Guid.NewGuid());
    }

    [Fact]
    public async Task RequireClinicAdminOrAdministratorAsync_ForbidsClinicAdminOfAnotherClinic()
    {
        var currentUser = new TestCurrentUser
        {
            Roles = ["clinicadmin"],
            AdministeredClinicIds = [Guid.NewGuid()]
        };

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CurrentUserGuard.RequireClinicAdminOrAdministratorAsync(currentUser, Guid.NewGuid()));
    }

    [Fact]
    public async Task RequirePatientAsync_ForbidsUserWithoutPatientProfile()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => CurrentUserGuard.RequirePatientAsync(new TestCurrentUser()));
    }

    [Fact]
    public async Task RequirePatientSelfAsync_ForbidsAnotherPatient()
    {
        var self = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CurrentUserGuard.RequirePatientSelfAsync(new TestCurrentUser { Patient = self }, Guid.NewGuid()));
    }

    [Fact]
    public async Task RequirePatientSelfOrAdministratorAsync_AllowsAdministratorForAnyPatient()
    {
        await CurrentUserGuard.RequirePatientSelfOrAdministratorAsync(
            new TestCurrentUser { Roles = ["admin"] },
            Guid.NewGuid());
    }

    [Fact]
    public async Task RequirePatientSelfOrAdministratorAsync_RejectsAnonymousUserAsUnauthorized()
    {
        var anonymous = new TestCurrentUser { IsAuthenticated = false, Subject = null };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CurrentUserGuard.RequirePatientSelfOrAdministratorAsync(anonymous, Guid.NewGuid()));
    }
}
