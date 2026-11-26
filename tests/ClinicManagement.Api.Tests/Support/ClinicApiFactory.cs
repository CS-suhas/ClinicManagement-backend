using System.Text;
using ClinicManagement.Application.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagement.Api.Tests.Support;

public sealed class ClinicApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://issuer.clinic.test/realms/clinic";
    public const string Audience = "clinic-api";
    public const string PlatformAdminSubject = "kc-platform-admin";

    private static readonly byte[] SigningKeyBytes =
        Encoding.UTF8.GetBytes("clinic-api-tests-signing-key-with-more-than-32-bytes");

    public InMemoryStore Store { get; } = new();

    public string MintToken(string subject, string? name = null, string? preferredUsername = null)
    {
        var claims = new List<System.Security.Claims.Claim> { new("sub", subject) };
        if (name is not null)
            claims.Add(new("name", name));
        if (preferredUsername is not null)
            claims.Add(new("preferred_username", preferredUsername));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new System.Security.Claims.ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(SigningKeyBytes),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting (not ConfigureAppConfiguration) so these values are visible while
        // Program.cs is still configuring services; with minimal hosting,
        // ConfigureAppConfiguration callbacks are only applied once the host is built.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "User Id=unused;Password=unused;Data Source=unused");
        builder.UseSetting("Authentication:Keycloak:Authority", Issuer);
        builder.UseSetting("Authentication:Keycloak:Audience", Audience);
        builder.UseSetting("Authentication:Keycloak:ValidIssuer", Issuer);
        builder.UseSetting("Authentication:Keycloak:RequireHttpsMetadata", "false");
        builder.UseSetting("Authorization:AdminSubjects:0", PlatformAdminSubject);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPatientRepository>();
            services.RemoveAll<IProviderRepository>();
            services.RemoveAll<IClinicRepository>();
            services.RemoveAll<IAppointmentRepository>();
            services.RemoveAll<IClinicAdminRepository>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IUserRoleRepository>();
            services.RemoveAll<IAllergyTypeRepository>();
            services.RemoveAll<IStatusRepository>();
            services.RemoveAll<ISearchRepository>();

            services.AddSingleton<IPatientRepository>(new InMemoryPatientRepository(Store));
            services.AddSingleton<IProviderRepository>(new InMemoryProviderRepository(Store));
            services.AddSingleton<IClinicRepository>(new InMemoryClinicRepository(Store));
            services.AddSingleton<IAppointmentRepository>(new InMemoryAppointmentRepository(Store));
            services.AddSingleton<IClinicAdminRepository>(new InMemoryClinicAdminRepository(Store));
            services.AddSingleton<IUserRepository>(new InMemoryUserRepository(Store));
            services.AddSingleton<IUserRoleRepository>(new InMemoryUserRoleRepository(Store));
            services.AddSingleton<IAllergyTypeRepository>(new InMemoryAllergyTypeRepository(Store));
            services.AddSingleton<IStatusRepository>(new InMemoryStatusRepository(Store));
            services.AddSingleton<ISearchRepository>(new InMemorySearchRepository(Store));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(
                    new OpenIdConnectConfiguration());
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(SigningKeyBytes);
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = Audience;
            });
        });
    }
}
