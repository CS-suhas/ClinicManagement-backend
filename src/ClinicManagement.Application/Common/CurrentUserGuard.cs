using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Common;

public static class CurrentUserGuard
{
    public static async Task<Provider> RequireVerifiedProviderAsync(ICurrentUser currentUser, Guid providerId)
    {
        EnsureAuthenticated(currentUser);

        var provider = await currentUser.GetProviderAsync()
            ?? throw new ForbiddenException("The signed-in user is not a registered provider.");
        if (provider.Id != providerId)
            throw new ForbiddenException($"Provider '{providerId}' is not the signed-in provider.");
        if (provider.VerificationStatus != ProviderVerificationStatus.VERIFIED)
            throw new ForbiddenException($"Provider '{providerId}' has not been verified.");

        return provider;
    }

    public static async Task RequireClinicAdminAsync(ICurrentUser currentUser, Guid clinicId)
    {
        EnsureAuthenticated(currentUser);

        if (!await currentUser.IsClinicAdminOfAsync(clinicId))
            throw new ForbiddenException($"The signed-in user does not administer clinic '{clinicId}'.");
    }

    private static void EnsureAuthenticated(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.Subject))
            throw new UnauthorizedAccessException("A signed-in user is required.");
    }
}
