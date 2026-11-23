using ClinicManagement.Application.ClinicJoinRequests.Interfaces;
using ClinicManagement.Application.ClinicJoinRequests.Models;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.ClinicJoinRequests.Services;

public class ClinicJoinRequestService(
    IClinicJoinRequestRepository requestRepository,
    IProviderRepository providerRepository,
    IClinicRepository clinicRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IClinicJoinRequestService
{
    public async Task<Guid> RequestToJoinAsync(Guid providerId, Guid clinicId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        var provider = await CurrentUserGuard.RequireVerifiedProviderAsync(currentUser, providerId);
        if (!await clinicRepository.ClinicExistsAsync(clinicId))
            throw new ResourceNotFoundException("Clinic", clinicId);

        if (provider.ClinicId is not null)
            throw new ProviderAlreadyInClinicException(providerId);
        if (await requestRepository.HasPendingAsync(providerId))
            throw new ClinicJoinRequestAlreadyPendingException(providerId);

        var request = ClinicJoinRequest.Create(Guid.NewGuid(), providerId, clinicId);
        return await requestRepository.AddAsync(request);
    }

    public async Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByProviderIdAsync(Guid providerId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        if (await providerRepository.GetProviderByIdAsync(providerId) is null)
            throw new ResourceNotFoundException("Provider", providerId);

        return await requestRepository.GetByProviderIdAsync(providerId);
    }

    public async Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByClinicIdAsync(
        Guid clinicId,
        ClinicJoinRequestStatus? status)
    {
        RequestValidator.EnsureGuid(clinicId, nameof(clinicId), "Clinic ID");

        if (!await clinicRepository.ClinicExistsAsync(clinicId))
            throw new ResourceNotFoundException("Clinic", clinicId);

        return await requestRepository.GetByClinicIdAsync(clinicId, status);
    }

    public async Task ApproveAsync(Guid requestId)
    {
        var request = await GetManagedRequestAsync(requestId);
        request.Approve(timeProvider.GetUtcNow());

        if (!await requestRepository.TryApproveAsync(request))
            throw new ConcurrentModificationException("Clinic join request", requestId);
    }

    public async Task RejectAsync(Guid requestId)
    {
        var request = await GetManagedRequestAsync(requestId);
        request.Reject(timeProvider.GetUtcNow());
        await PersistDecisionAsync(request);
    }

    public async Task CancelAsync(Guid requestId)
    {
        var request = await GetRequiredAsync(requestId);
        request.Cancel(timeProvider.GetUtcNow());
        await PersistDecisionAsync(request);
    }

    private async Task PersistDecisionAsync(ClinicJoinRequest request)
    {
        if (!await requestRepository.TryDecideAsync(request))
            throw new ConcurrentModificationException("Clinic join request", request.Id);
    }

    private async Task<ClinicJoinRequest> GetManagedRequestAsync(Guid requestId)
    {
        var request = await GetRequiredAsync(requestId);
        await CurrentUserGuard.RequireClinicAdminAsync(currentUser, request.ClinicId);
        return request;
    }

    private async Task<ClinicJoinRequest> GetRequiredAsync(Guid requestId)
    {
        RequestValidator.EnsureGuid(requestId, nameof(requestId), "Join request ID");

        return await requestRepository.GetByIdAsync(requestId)
            ?? throw new ResourceNotFoundException("Clinic join request", requestId);
    }
}
