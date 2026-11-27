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
    Task<IReadOnlyDictionary<Guid, int>> GetVerifiedProviderCountsByClinicAsync();
    Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus = null);
    Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId);
    Task<(IReadOnlyList<ProviderPatientRosterItem> Items, int TotalCount)> SearchPatientRosterAsync(
        Guid providerId,
        PageQuery query,
        int offset,
        DateTimeOffset now);
    Task<bool> TryUpdateVerificationStatusAsync(
        Guid providerId,
        ProviderVerificationStatus expected,
        ProviderVerificationStatus next);
    Task<Provider?> GetByUserIdAsync(Guid userId);
}
