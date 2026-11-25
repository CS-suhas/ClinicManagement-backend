using Microsoft.Extensions.Options;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Application.Common;
using System.Security.Claims;
using ClinicManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
        services.AddScoped<UserAccessContextHolder>();
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

        services.AddOptions<AuthorizationSettings>().BindConfiguration(AuthorizationSettings.SectionName);
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<AuthorizationSettings>>().Value);
        services.AddOptions<PublicAccessSettings>().BindConfiguration(PublicAccessSettings.SectionName);
        services.AddSingleton<IAuthorizationHandler, PublicDoctorsAuthorizationHandler>();

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        var authorization = services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.Authenticated, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(AuthorizationPolicies.PublicDoctors, policy =>
                policy.AddRequirements(new PublicDoctorsRequirement()));

        foreach (var permission in Permissions.All)
        {
            authorization.AddPolicy(permission, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission)));
        }

        return services;
    }
}
