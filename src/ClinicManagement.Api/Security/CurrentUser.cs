using System.Security.Claims;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Api.Security;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    IPatientRepository patientRepository,
    IProviderRepository providerRepository,
    IClinicAdminRepository clinicAdminRepository) : ICurrentUser
{
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

    public async Task<Patient?> GetPatientAsync()
    {
        return UserId is { } userId ? await patientRepository.GetByUserIdAsync(userId) : null;
    }

    public async Task<Provider?> GetProviderAsync()
    {
        return UserId is { } userId ? await providerRepository.GetByUserIdAsync(userId) : null;
    }

    public async Task<bool> IsClinicAdminOfAsync(Guid clinicId)
    {
        return UserId is { } userId && await clinicAdminRepository.IsAdminOfClinicAsync(userId, clinicId);
    }

    public async Task<IReadOnlyList<Guid>> GetAdministeredClinicIdsAsync()
    {
        return UserId is { } userId ? await clinicAdminRepository.GetClinicIdsAsync(userId) : [];
    }
}
