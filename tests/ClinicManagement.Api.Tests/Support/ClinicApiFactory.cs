using System.Text;
using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.ClinicJoinRequests.Interfaces;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
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

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "User Id=unused;Password=unused;Data Source=unused",
            ["Authentication:Keycloak:Authority"] = Issuer,
            ["Authentication:Keycloak:Audience"] = Audience,
            ["Authentication:Keycloak:ValidIssuer"] = Issuer,
            ["Authentication:Keycloak:RequireHttpsMetadata"] = "false",
            ["Authorization:AdminSubjects:0"] = PlatformAdminSubject
        }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPatientRepository>();
            services.RemoveAll<IProviderRepository>();
            services.RemoveAll<IClinicRepository>();
            services.RemoveAll<IAppointmentRepository>();
            services.RemoveAll<IClinicJoinRequestRepository>();
            services.RemoveAll<IClinicAdminRepository>();
            services.RemoveAll<IPlatformAdminRepository>();
            services.RemoveAll<IAllergyTypeRepository>();
            services.RemoveAll<IStatusRepository>();

            services.AddSingleton<IPatientRepository>(new InMemoryPatientRepository(Store));
            services.AddSingleton<IProviderRepository>(new InMemoryProviderRepository(Store));
            services.AddSingleton<IClinicRepository>(new InMemoryClinicRepository(Store));
            services.AddSingleton<IAppointmentRepository>(new InMemoryAppointmentRepository(Store));
            services.AddSingleton<IClinicJoinRequestRepository>(new InMemoryClinicJoinRequestRepository(Store));
            services.AddSingleton<IClinicAdminRepository>(new InMemoryClinicAdminRepository(Store));
            services.AddSingleton<IPlatformAdminRepository>(new InMemoryPlatformAdminRepository(Store));
            services.AddSingleton<IAllergyTypeRepository>(new InMemoryAllergyTypeRepository(Store));
            services.AddSingleton<IStatusRepository>(new InMemoryStatusRepository(Store));

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
