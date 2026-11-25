using ClinicManagement.Application.ClinicJoinRequests.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.ClinicJoinRequests.Interfaces;

public interface IClinicJoinRequestRepository
{
    Task<ClinicJoinRequest?> GetByIdAsync(Guid requestId);
    Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByProviderIdAsync(Guid providerId);
    Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByClinicIdAsync(
        Guid clinicId,
        ClinicJoinRequestStatus? status);
    Task<bool> HasPendingAsync(Guid providerId);
    /// <exception cref="ClinicManagement.Domain.Exceptions.ClinicJoinRequestAlreadyPendingException">
    /// The provider already has a pending request.</exception>
    Task<Guid> AddAsync(ClinicJoinRequest request);
    /// <summary>
    /// Persists an approval (the request was already moved to APPROVED in memory) and moves the provider
    /// to the clinic, in one transaction. Returns false, changing nothing, when the stored request is no
    /// longer pending, the provider is no longer verified, or the provider is already at that clinic.
    /// </summary>
    Task<bool> TryApproveAsync(ClinicJoinRequest approvedRequest);
    /// <summary>
    /// Persists a rejection or cancellation (already applied in memory). Returns false when the stored
    /// request is no longer pending.
    /// </summary>
    Task<bool> TryDecideAsync(ClinicJoinRequest decidedRequest);
}
