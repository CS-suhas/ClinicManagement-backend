using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Tests;

public class ProviderVerificationTests
{
    [Fact]
    public void Register_CreatesPendingProviderWithoutClinic()
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");

        Assert.Equal(ProviderVerificationStatus.PENDING, provider.VerificationStatus);
        Assert.Null(provider.ClinicId);
        Assert.False(provider.IsAcceptingAppointments);
    }

    [Fact]
    public void Create_ForAdminManagedProvider_IsVerifiedAndAcceptingAppointments()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        Assert.Equal(ProviderVerificationStatus.VERIFIED, provider.VerificationStatus);
        Assert.True(provider.IsAcceptingAppointments);
    }

    [Fact]
    public void Register_RejectsBlankSpecialty()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Provider.Register(Guid.NewGuid(), "Grace", "Hopper", " "));

        Assert.Equal("specialty", exception.PropertyName);
    }

    [Fact]
    public void Verify_MovesPendingProviderToVerified_ButStillNeedsAClinic()
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");

        provider.Verify();

        Assert.Equal(ProviderVerificationStatus.VERIFIED, provider.VerificationStatus);
        Assert.False(provider.IsAcceptingAppointments);
    }

    [Fact]
    public void Verify_AllowsReconsideringARejectedProvider()
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        provider.Reject();

        provider.Verify();

        Assert.Equal(ProviderVerificationStatus.VERIFIED, provider.VerificationStatus);
    }

    [Fact]
    public void Verify_ThrowsWhenAlreadyVerified()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        Assert.Throws<InvalidStatusTransitionException>(() => provider.Verify());
    }

    [Fact]
    public void Reject_ThrowsUnlessPending()
    {
        var verified = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var rejected = Provider.Register(Guid.NewGuid(), "Alan", "Turing", "Computing");
        rejected.Reject();

        Assert.Throws<InvalidStatusTransitionException>(() => verified.Reject());
        Assert.Throws<InvalidStatusTransitionException>(() => rejected.Reject());
    }
}
