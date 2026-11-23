using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Common;

public static class CurrentUserGuard
{
    private const string AdminRole = "admin";

    public static void RequireAuthenticated(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.Subject))
            throw new UnauthorizedAccessException("A signed-in user is required.");
    }

    public static string RequireSubject(ICurrentUser currentUser) =>
        string.IsNullOrWhiteSpace(currentUser.Subject)
            ? throw new UnauthorizedAccessException("A signed-in user is required.")
            : currentUser.Subject;

    public static Guid RequireUserId(ICurrentUser currentUser)
    {
        RequireAuthenticated(currentUser);
        return currentUser.UserId ?? throw new UnauthorizedAccessException("A signed-in user is required.");
    }

    public static bool IsAdministrator(ICurrentUser currentUser) => currentUser.IsInRole(AdminRole);

    public static async Task<Provider> RequireProviderAsync(ICurrentUser currentUser, Guid providerId)
    {
        RequireAuthenticated(currentUser);

        var provider = await currentUser.GetProviderAsync()
            ?? throw new ForbiddenException("The signed-in user is not a registered provider.");
        if (provider.Id != providerId)
            throw new ForbiddenException($"Provider '{providerId}' is not the signed-in provider.");

        return provider;
    }

    public static async Task<Provider> RequireVerifiedProviderAsync(ICurrentUser currentUser, Guid providerId)
    {
        var provider = await RequireProviderAsync(currentUser, providerId);
        if (provider.VerificationStatus != ProviderVerificationStatus.VERIFIED)
            throw new ForbiddenException($"Provider '{providerId}' has not been verified.");

        return provider;
    }

    public static async Task RequireProviderOrAdministratorAsync(ICurrentUser currentUser, Guid providerId)
    {
        RequireAuthenticated(currentUser);
        if (IsAdministrator(currentUser))
            return;

        await RequireProviderAsync(currentUser, providerId);
    }

    public static async Task RequireClinicAdminAsync(ICurrentUser currentUser, Guid clinicId)
    {
        RequireAuthenticated(currentUser);

        if (!await currentUser.IsClinicAdminOfAsync(clinicId))
            throw new ForbiddenException($"The signed-in user does not administer clinic '{clinicId}'.");
    }

    public static async Task RequireClinicAdminOrAdministratorAsync(ICurrentUser currentUser, Guid clinicId)
    {
        RequireAuthenticated(currentUser);
        if (IsAdministrator(currentUser))
            return;

        await RequireClinicAdminAsync(currentUser, clinicId);
    }

    public static async Task<Patient> RequirePatientAsync(ICurrentUser currentUser)
    {
        RequireAuthenticated(currentUser);

        return await currentUser.GetPatientAsync()
            ?? throw new ForbiddenException("The signed-in user is not a registered patient.");
    }

    public static async Task RequirePatientSelfAsync(ICurrentUser currentUser, Guid patientId)
    {
        var patient = await RequirePatientAsync(currentUser);
        if (patient.Id != patientId)
            throw new ForbiddenException($"Patient '{patientId}' is not the signed-in patient.");
    }

    public static async Task RequirePatientSelfOrAdministratorAsync(ICurrentUser currentUser, Guid patientId)
    {
        RequireAuthenticated(currentUser);
        if (IsAdministrator(currentUser))
            return;

        await RequirePatientSelfAsync(currentUser, patientId);
    }
}
