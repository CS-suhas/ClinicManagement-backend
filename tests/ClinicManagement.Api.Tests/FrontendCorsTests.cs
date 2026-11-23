using ClinicManagement.Api.Security;

namespace ClinicManagement.Api.Tests;

public sealed class FrontendCorsTests
{
    [Fact]
    public void Development_DefaultsToLocalFrontendOrigin()
    {
        var origins = FrontendCors.ResolveAllowedOrigins(null, isDevelopment: true);

        Assert.Equal([FrontendCors.DevelopmentOrigin], origins);
    }

    [Fact]
    public void NonDevelopment_RequiresExplicitOrigins()
    {
        Assert.Throws<InvalidOperationException>(() => FrontendCors.ResolveAllowedOrigins(null, isDevelopment: false));
        Assert.Throws<InvalidOperationException>(() => FrontendCors.ResolveAllowedOrigins("  ", isDevelopment: false));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.clinic.example")]
    [InlineData("https://app.clinic.example;*")]
    public void Wildcards_AreRejectedInEveryEnvironment(string configured)
    {
        Assert.Throws<InvalidOperationException>(() => FrontendCors.ResolveAllowedOrigins(configured, isDevelopment: true));
        Assert.Throws<InvalidOperationException>(() => FrontendCors.ResolveAllowedOrigins(configured, isDevelopment: false));
    }

    [Theory]
    [InlineData("https://app.clinic.example/")]
    [InlineData("https://app.clinic.example/dashboard")]
    [InlineData("ftp://app.clinic.example")]
    [InlineData("not-a-url")]
    public void Entries_MustBeAbsoluteHttpOrigins(string configured)
    {
        Assert.Throws<InvalidOperationException>(() => FrontendCors.ResolveAllowedOrigins(configured, isDevelopment: false));
    }

    [Fact]
    public void ExplicitOrigins_AreParsedFromSemicolonList()
    {
        var origins = FrontendCors.ResolveAllowedOrigins(
            " https://app.clinic.example ; http://localhost:3000 ",
            isDevelopment: false);

        Assert.Equal(["https://app.clinic.example", "http://localhost:3000"], origins);
    }
}
