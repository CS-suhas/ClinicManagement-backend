using System.Security.Claims;
using ClinicManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ClinicManagement.Api.Security;

public static class AuthenticationExtensions
{
    private const string ConfigSection = "Authentication:Keycloak";

    public static IServiceCollection AddClinicAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloak = configuration.GetSection(ConfigSection);
        var authority = keycloak["Authority"]
            ?? throw new InvalidOperationException($"'{ConfigSection}:Authority' is not configured.");
        var audience = keycloak["Audience"]
            ?? throw new InvalidOperationException($"'{ConfigSection}:Audience' is not configured.");
        var requireHttpsMetadata = keycloak.GetValue("RequireHttpsMetadata", true);
        var validIssuer = keycloak["ValidIssuer"];

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = validIssuer,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "preferred_username"
                };
            });

        services.AddScoped<IClaimsTransformation, RoleClaimsTransformation>();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Authenticated, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AuthorizationPolicies.PatientOnly, policy => policy.RequireRole(AuthorizationPolicies.PatientRole))
            .AddPolicy(AuthorizationPolicies.DoctorOnly, policy => policy.RequireRole(AuthorizationPolicies.DoctorRole))
            .AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(AuthorizationPolicies.AdminRole))
            .AddPolicy(AuthorizationPolicies.ClinicAdminOnly, policy =>
                policy.RequireRole(AuthorizationPolicies.ClinicAdminRole))
            .AddPolicy(AuthorizationPolicies.ClinicAdminOrAdministratorOnly, policy =>
                policy.RequireRole(AuthorizationPolicies.AdminRole, AuthorizationPolicies.ClinicAdminRole));

        return services;
    }
}
