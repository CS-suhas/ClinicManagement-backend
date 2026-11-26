using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Repositories;

public interface IProviderRepository
{
    Task<Provider?> GetProviderByIdAsync(Guid providerId);
    Task<IEnumerable<Provider>> GetAllProvidersAsync();
    Task<Guid> AddProviderAsync(Provider request, Guid clinicId);
    Task<Guid> UpdateProviderAsync(Provider request, Guid clinicId);
    Task<Guid> DeleteProviderAsync(Guid providerId);
    Task<bool> HasAppointmentsAsync(Guid providerId);
    Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId);
    /// <summary>Number of VERIFIED providers at each clinic that has at least one. Clinics with none are absent.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetVerifiedProviderCountsByClinicAsync();
    Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus = null);
    /// <summary>Providers that patients may see: verified and assigned to a clinic.</summary>
    Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId);
    /// <summary>
    /// Patients with at least one appointment with the provider, matched by name when searched. Last and next
    /// visits are relative to <paramref name="now"/> and ignore declined and cancelled appointments.
    /// </summary>
    Task<(IReadOnlyList<ProviderPatientRosterItem> Items, int TotalCount)> SearchPatientRosterAsync(
        Guid providerId,
        PageQuery query,
        int offset,
        DateTimeOffset now);
    /// <summary>Returns false when the stored status is no longer <paramref name="expected"/>.</summary>
    Task<bool> TryUpdateVerificationStatusAsync(
        Guid providerId,
        ProviderVerificationStatus expected,
        ProviderVerificationStatus next);
    Task<Provider?> GetByUserIdAsync(Guid userId);
}
