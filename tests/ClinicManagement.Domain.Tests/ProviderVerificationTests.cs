using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Tests;

public class ProviderVerificationTests
{
    [Fact]
    public void Register_CreatesPendingProviderAtTheChosenClinic()
    {
        var clinicId = Guid.NewGuid();
        var provider = Provider.Register(Guid.NewGuid(), clinicId, "Grace", "Hopper", "Computing");

        Assert.Equal(ProviderVerificationStatus.PENDING, provider.VerificationStatus);
        Assert.Equal(clinicId, provider.ClinicId);
        Assert.False(provider.IsAcceptingAppointments);
    }

    [Fact]
    public void Register_RejectsEmptyClinicId()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Provider.Register(Guid.NewGuid(), Guid.Empty, "Grace", "Hopper", "Computing"));

        Assert.Equal("clinicId", exception.PropertyName);
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
            () => Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", " "));

        Assert.Equal("specialty", exception.PropertyName);
    }

    [Fact]
    public void Verify_MovesPendingProviderToVerified_AndAcceptingAppointments()
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        provider.Verify();

        Assert.Equal(ProviderVerificationStatus.VERIFIED, provider.VerificationStatus);
        Assert.True(provider.IsAcceptingAppointments);
    }

    [Fact]
    public void Verify_AllowsReconsideringARejectedProvider()
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
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
        var rejected = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Alan", "Turing", "Computing");
        rejected.Reject();

        Assert.Throws<InvalidStatusTransitionException>(() => verified.Reject());
        Assert.Throws<InvalidStatusTransitionException>(() => rejected.Reject());
    }

    [Fact]
    public void FeeAndGender_AreOptionalAndUnsetByDefault()
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        Assert.Null(provider.Fee);
        Assert.Null(provider.Gender);
    }

    [Fact]
    public void SetFeeAndGender_StoresValues_AndCanClearThem()
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        provider.SetFee(2500.50m);
        provider.SetGender(Gender.FEMALE);
        Assert.Equal(2500.50m, provider.Fee);
        Assert.Equal(Gender.FEMALE, provider.Gender);

        provider.SetFee(null);
        provider.SetGender(null);
        Assert.Null(provider.Fee);
        Assert.Null(provider.Gender);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("10.005")]
    [InlineData("100000000")]
    public void SetFee_RejectsNegativeOverlongOrTooPreciseValues(string fee)
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        var exception = Assert.Throws<DomainValidationException>(
            () => provider.SetFee(decimal.Parse(fee, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal("fee", exception.PropertyName);
    }

    [Fact]
    public void SetFee_AllowsZero()
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        provider.SetFee(0m);

        Assert.Equal(0m, provider.Fee);
    }

    [Fact]
    public void SetGender_RejectsUndefinedValue()
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");

        Assert.Throws<DomainValidationException>(() => provider.SetGender((Gender)42));
    }
}
