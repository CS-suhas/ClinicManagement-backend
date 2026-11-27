using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ClinicManagement.Api.Security;

public sealed class PublicDoctorsRequirement : IAuthorizationRequirement;

public sealed class PublicDoctorsAuthorizationHandler(IOptionsMonitor<PublicAccessSettings> settings)
    : AuthorizationHandler<PublicDoctorsRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PublicDoctorsRequirement requirement)
    {
        if (settings.CurrentValue.Doctors || context.User.Identity?.IsAuthenticated == true)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
