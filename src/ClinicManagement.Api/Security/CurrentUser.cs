using System.Security.Claims;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Api.Security;

/// <summary>
/// The signed-in user for the current request. Role and ownership answers come from the
/// <see cref="UserAccessContext"/> loaded once during authentication; the linked patient and provider entities
/// are fetched at most once, and only when the context says they exist.
/// </summary>
public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    UserAccessContextHolder holder,
    IUserRepository userRepository,
    IPatientRepository patientRepository,
    IProviderRepository providerRepository) : ICurrentUser
{
    private Task<Patient?>? patient;
    private Task<Provider?>? provider;

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public string? Subject =>
        Principal?.FindFirst("sub")?.Value ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst(ClinicClaimTypes.UserId)?.Value, out var userId) ? userId : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        ?? (IReadOnlyCollection<string>)[];

    public string? DisplayName
    {
        get
        {
            var name = Principal?.FindFirst("name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
                name = Principal?.FindFirst("preferred_username")?.Value;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public Task<Patient?> GetPatientAsync() => patient ??= LoadPatientAsync();

    public Task<Provider?> GetProviderAsync() => provider ??= LoadProviderAsync();

    public async Task<bool> IsClinicAdminOfAsync(Guid clinicId) =>
        (await GetContextAsync())?.AdministeredClinicIds.Contains(clinicId) == true;

    public async Task<IReadOnlyList<Guid>> GetAdministeredClinicIdsAsync() =>
        (await GetContextAsync())?.AdministeredClinicIds ?? [];

    private async Task<Patient?> LoadPatientAsync() =>
        await GetContextAsync() is { PatientId: not null } context
            ? await patientRepository.GetByUserIdAsync(context.UserId)
            : null;

    private async Task<Provider?> LoadProviderAsync() =>
        await GetContextAsync() is { ProviderId: not null } context
            ? await providerRepository.GetByUserIdAsync(context.UserId)
            : null;

    /// <summary>
    /// The context set during authentication, or loaded here should authentication not have filled it.
    /// Null when nobody is signed in.
    /// </summary>
    private async Task<UserAccessContext?> GetContextAsync()
    {
        if (UserId is not { } userId || Subject is not { } subject)
            return null;
        if (holder.Context is { } context && context.UserId == userId)
            return context;

        context = await userRepository.GetAccessContextAsync(userId);
        holder.Set(subject, context);
        return context;
    }
}
