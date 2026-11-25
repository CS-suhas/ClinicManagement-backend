using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Common;

/// <summary>
/// Authorization checks on the signed-in user. Each <c>Require...</c> method throws
/// <see cref="UnauthorizedAccessException"/> when nobody is signed in and <see cref="ForbiddenException"/> when the
/// signed-in user may not act.
/// </summary>
public static class CurrentUserGuard
{
    public static void RequireAuthenticated(this ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.Subject))
            throw new UnauthorizedAccessException("A signed-in user is required.");
    }

    public static string RequireSubject(this ICurrentUser currentUser) =>
        string.IsNullOrWhiteSpace(currentUser.Subject)
            ? throw new UnauthorizedAccessException("A signed-in user is required.")
            : currentUser.Subject;

    public static Guid RequireUserId(this ICurrentUser currentUser)
    {
        currentUser.RequireAuthenticated();
        return currentUser.UserId ?? throw new UnauthorizedAccessException("A signed-in user is required.");
    }

    public static bool IsAdministrator(this ICurrentUser currentUser) => currentUser.IsInRole(Roles.Admin);

    public static async Task<Provider> RequireProviderAsync(this ICurrentUser currentUser, Guid providerId)
    {
        currentUser.RequireAuthenticated();

        var provider = await currentUser.GetProviderAsync()
            ?? throw new ForbiddenException("The signed-in user is not a registered provider.");
        if (provider.Id != providerId)
            throw new ForbiddenException($"Provider '{providerId}' is not the signed-in provider.");

        return provider;
    }

    public static async Task<Provider> RequireVerifiedProviderAsync(this ICurrentUser currentUser, Guid providerId)
    {
        var provider = await currentUser.RequireProviderAsync(providerId);
        if (provider.VerificationStatus != ProviderVerificationStatus.VERIFIED)
            throw new ForbiddenException($"Provider '{providerId}' has not been verified.");

        return provider;
    }

    public static async Task RequireProviderOrAdministratorAsync(this ICurrentUser currentUser, Guid providerId)
    {
        currentUser.RequireAuthenticated();
        if (currentUser.IsAdministrator())
            return;

        await currentUser.RequireProviderAsync(providerId);
    }

    public static async Task RequireClinicAdminAsync(this ICurrentUser currentUser, Guid clinicId)
    {
        currentUser.RequireAuthenticated();

        if (!await currentUser.IsClinicAdminOfAsync(clinicId))
            throw new ForbiddenException($"The signed-in user does not administer clinic '{clinicId}'.");
    }

    public static async Task RequireClinicAdminOrAdministratorAsync(this ICurrentUser currentUser, Guid clinicId)
    {
        currentUser.RequireAuthenticated();
        if (currentUser.IsAdministrator())
            return;

        await currentUser.RequireClinicAdminAsync(clinicId);
    }

    public static async Task<Patient> RequirePatientAsync(this ICurrentUser currentUser)
    {
        currentUser.RequireAuthenticated();

        return await currentUser.GetPatientAsync()
            ?? throw new ForbiddenException("The signed-in user is not a registered patient.");
    }

    public static async Task RequirePatientSelfAsync(this ICurrentUser currentUser, Guid patientId)
    {
        var patient = await currentUser.RequirePatientAsync();
        if (patient.Id != patientId)
            throw new ForbiddenException($"Patient '{patientId}' is not the signed-in patient.");
    }

    public static async Task RequirePatientSelfOrAdministratorAsync(this ICurrentUser currentUser, Guid patientId)
    {
        currentUser.RequireAuthenticated();
        if (currentUser.IsAdministrator())
            return;

        await currentUser.RequirePatientSelfAsync(patientId);
    }
}
