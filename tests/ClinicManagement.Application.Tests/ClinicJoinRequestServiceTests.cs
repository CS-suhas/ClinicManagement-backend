using ClinicManagement.Application.ClinicJoinRequests.Interfaces;
using ClinicManagement.Application.ClinicJoinRequests.Models;
using ClinicManagement.Application.ClinicJoinRequests.Services;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class ClinicJoinRequestServiceTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static Provider VerifiedUnassignedProvider()
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        provider.Verify();
        return provider;
    }

    [Fact]
    public async Task GetByClinicIdAsync_RejectsUserWhoDoesNotAdministerTheClinic()
    {
        var clinicId = Guid.NewGuid();
        var (service, _) = CreateService(currentUser: new TestCurrentUser());

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByClinicIdAsync(clinicId, null));
    }

    [Fact]
    public async Task CancelAsync_RejectsProviderThatDidNotRequestTheJoin()
    {
        var cancellingProvider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var toCancel = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (service, requests) = CreateService(cancellingProvider, requests: new FakeRequestRepository { Existing = toCancel });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelAsync(toCancel.Id));

        Assert.Null(requests.Decided);
    }

    [Fact]
    public async Task GetByProviderIdAsync_ForbidsProviderOtherThanTheCaller()
    {
        var caller = VerifiedUnassignedProvider();
        var (service, _) = CreateService(currentUser: new TestCurrentUser { Provider = caller });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByProviderIdAsync(Guid.NewGuid()));
    }

    private static TestCurrentUser AdministratorOf(Guid clinicId) =>
        new() { AdministeredClinicIds = [clinicId] };

    private static (ClinicJoinRequestService Service, FakeRequestRepository Requests) CreateService(
        Provider? provider = null,
        bool clinicExists = true,
        FakeRequestRepository? requests = null,
        TestCurrentUser? currentUser = null)
    {
        requests ??= new FakeRequestRepository();
        var service = new ClinicJoinRequestService(
            requests,
            new FakeProviderRepository(provider),
            new FakeClinicRepository(clinicExists),
            currentUser ?? new TestCurrentUser { Provider = provider },
            new FixedTimeProvider(Now));
        return (service, requests);
    }

    [Fact]
    public async Task RequestToJoinAsync_CreatesPendingRequestForVerifiedUnassignedProvider()
    {
        var provider = VerifiedUnassignedProvider();
        var clinicId = Guid.NewGuid();
        var (service, requests) = CreateService(provider);

        var id = await service.RequestToJoinAsync(provider.Id, clinicId);

        var added = Assert.IsType<ClinicJoinRequest>(requests.Added);
        Assert.Equal(id, added.Id);
        Assert.Equal(provider.Id, added.ProviderId);
        Assert.Equal(clinicId, added.ClinicId);
        Assert.Equal(ClinicJoinRequestStatus.PENDING, added.Status);
    }

    [Fact]
    public async Task RequestToJoinAsync_ForbidsUnverifiedProvider()
    {
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var (service, requests) = CreateService(provider);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.RequestToJoinAsync(provider.Id, Guid.NewGuid()));

        Assert.Null(requests.Added);
    }

    [Fact]
    public async Task RequestToJoinAsync_RejectsProviderThatAlreadyHasAClinic()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var (service, requests) = CreateService(provider);

        await Assert.ThrowsAsync<ProviderAlreadyInClinicException>(
            () => service.RequestToJoinAsync(provider.Id, Guid.NewGuid()));

        Assert.Null(requests.Added);
    }

    [Fact]
    public async Task RequestToJoinAsync_RejectsSecondPendingRequest()
    {
        var provider = VerifiedUnassignedProvider();
        var (service, requests) = CreateService(provider, requests: new FakeRequestRepository { HasPending = true });

        await Assert.ThrowsAsync<ClinicJoinRequestAlreadyPendingException>(
            () => service.RequestToJoinAsync(provider.Id, Guid.NewGuid()));

        Assert.Null(requests.Added);
    }

    [Fact]
    public async Task RequestToJoinAsync_ForbidsRequestForAnotherProvider()
    {
        var provider = VerifiedUnassignedProvider();
        var (service, requests) = CreateService(provider);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.RequestToJoinAsync(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Null(requests.Added);
    }

    [Fact]
    public async Task RequestToJoinAsync_ThrowsNotFoundForUnknownClinic()
    {
        var provider = VerifiedUnassignedProvider();
        var (service, _) = CreateService(provider, clinicExists: false);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.RequestToJoinAsync(provider.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task ApproveAsync_PersistsApprovalWithInjectedTime()
    {
        var clinicId = Guid.NewGuid();
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), clinicId);
        var (service, requests) = CreateService(
            requests: new FakeRequestRepository { Existing = request },
            currentUser: AdministratorOf(clinicId));

        await service.ApproveAsync(request.Id);

        Assert.Same(request, requests.Approved);
        Assert.Equal(ClinicJoinRequestStatus.APPROVED, request.Status);
        Assert.Equal(Now, request.DecidedAtUtc);
    }

    [Fact]
    public async Task ApproveAsync_ForbidsCallerThatDoesNotAdministerClinic()
    {
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (service, requests) = CreateService(
            requests: new FakeRequestRepository { Existing = request },
            currentUser: AdministratorOf(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ApproveAsync(request.Id));

        Assert.Null(requests.Approved);
    }

    [Fact]
    public async Task ApproveAsync_RequiresSignedInUser()
    {
        var clinicId = Guid.NewGuid();
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), clinicId);
        var (service, requests) = CreateService(
            requests: new FakeRequestRepository { Existing = request },
            currentUser: new TestCurrentUser { IsAuthenticated = false, AdministeredClinicIds = [clinicId] });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveAsync(request.Id));

        Assert.Null(requests.Approved);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsConflictWhenRequestWasAlreadyDecided()
    {
        var clinicId = Guid.NewGuid();
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), clinicId);
        request.Reject(Now);
        var (service, requests) = CreateService(
            requests: new FakeRequestRepository { Existing = request },
            currentUser: AdministratorOf(clinicId));

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => service.ApproveAsync(request.Id));

        Assert.Null(requests.Approved);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsConflictWhenAnotherRequestWonTheRace()
    {
        var clinicId = Guid.NewGuid();
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), clinicId);
        var (service, _) = CreateService(
            requests: new FakeRequestRepository { Existing = request, WriteSucceeds = false },
            currentUser: AdministratorOf(clinicId));

        await Assert.ThrowsAsync<ConcurrentModificationException>(() => service.ApproveAsync(request.Id));
    }

    [Fact]
    public async Task RejectAsync_And_CancelAsync_PersistTheDecision()
    {
        var clinicId = Guid.NewGuid();
        var toReject = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), clinicId);
        var (rejectService, rejectRequests) = CreateService(
            requests: new FakeRequestRepository { Existing = toReject },
            currentUser: AdministratorOf(clinicId));
        var cancellingProvider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var toCancel = ClinicJoinRequest.Create(Guid.NewGuid(), cancellingProvider.Id, Guid.NewGuid());
        var (cancelService, cancelRequests) = CreateService(
            cancellingProvider,
            requests: new FakeRequestRepository { Existing = toCancel });

        await rejectService.RejectAsync(toReject.Id);
        await cancelService.CancelAsync(toCancel.Id);

        Assert.Equal(ClinicJoinRequestStatus.REJECTED, rejectRequests.Decided?.Status);
        Assert.Equal(ClinicJoinRequestStatus.CANCELLED, cancelRequests.Decided?.Status);
    }

    [Fact]
    public async Task RejectAsync_ForbidsCallerThatDoesNotAdministerClinic()
    {
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (service, requests) = CreateService(
            requests: new FakeRequestRepository { Existing = request },
            currentUser: AdministratorOf(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.RejectAsync(request.Id));

        Assert.Null(requests.Decided);
    }

    [Fact]
    public async Task ApproveAsync_ThrowsNotFoundForUnknownRequest()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.ApproveAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ApproveAsync_AllowsPlatformAdministrator()
    {
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (service, requests) = CreateService(
            requests: new FakeRequestRepository { Existing = request },
            currentUser: new TestCurrentUser { Roles = ["admin"] });

        await service.ApproveAsync(request.Id);

        Assert.Same(request, requests.Approved);
        Assert.Equal(ClinicJoinRequestStatus.APPROVED, request.Status);
    }

    [Fact]
    public async Task RejectAsync_AllowsPlatformAdministrator()
    {
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (service, requests) = CreateService(
            requests: new FakeRequestRepository { Existing = request },
            currentUser: new TestCurrentUser { Roles = ["admin"] });

        await service.RejectAsync(request.Id);

        Assert.Equal(ClinicJoinRequestStatus.REJECTED, requests.Decided?.Status);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeRequestRepository : IClinicJoinRequestRepository
    {
        public ClinicJoinRequest? Existing { get; init; }
        public bool HasPending { get; init; }
        public bool WriteSucceeds { get; init; } = true;
        public ClinicJoinRequest? Added { get; private set; }
        public ClinicJoinRequest? Approved { get; private set; }
        public ClinicJoinRequest? Decided { get; private set; }

        public Task<ClinicJoinRequest?> GetByIdAsync(Guid requestId) =>
            Task.FromResult(Existing?.Id == requestId ? Existing : null);
        public Task<bool> HasPendingAsync(Guid providerId) => Task.FromResult(HasPending);
        public Task<Guid> AddAsync(ClinicJoinRequest request)
        {
            Added = request;
            return Task.FromResult(request.Id);
        }
        public Task<bool> TryApproveAsync(ClinicJoinRequest approvedRequest)
        {
            if (WriteSucceeds) Approved = approvedRequest;
            return Task.FromResult(WriteSucceeds);
        }
        public Task<bool> TryDecideAsync(ClinicJoinRequest decidedRequest)
        {
            if (WriteSucceeds) Decided = decidedRequest;
            return Task.FromResult(WriteSucceeds);
        }
        public Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByProviderIdAsync(Guid providerId) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByClinicIdAsync(
            Guid clinicId,
            ClinicJoinRequestStatus? status) => throw new NotSupportedException();
    }

    private sealed class FakeProviderRepository(Provider? provider) : IProviderRepository
    {
        public Task<IReadOnlyDictionary<Guid, int>> GetVerifiedProviderCountsByClinicAsync() =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<ClinicManagement.Application.Models.ProviderPatientRosterItem> Items, int TotalCount)>
            SearchPatientRosterAsync(
                Guid providerId,
                ClinicManagement.Application.Models.PageQuery query,
                int offset,
                DateTimeOffset now) => throw new NotSupportedException();
        public Task<Provider?> GetByIdentitySubjectAsync(string identitySubject) => Task.FromResult<Provider?>(null);
        public Task<Provider?> GetProviderByIdAsync(Guid providerId) =>
            Task.FromResult(provider?.Id == providerId ? provider : null);
        public Task<IEnumerable<Provider>> GetAllProvidersAsync() => throw new NotSupportedException();
        public Task<Guid> AddProviderAsync(Provider request, Guid clinicId) => throw new NotSupportedException();
        public Task<Guid> UpdateProviderAsync(Provider request, Guid clinicId) => throw new NotSupportedException();
        public Task<Guid> DeleteProviderAsync(Guid providerId) => throw new NotSupportedException();
        public Task<bool> HasAppointmentsAsync(Guid providerId) => throw new NotSupportedException();
        public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
            PageQuery query,
            int offset,
            Guid? clinicId,
            ProviderVerificationStatus? verificationStatus = null) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
            PageQuery query,
            int offset,
            Guid? clinicId) => throw new NotSupportedException();
        public Task<Guid> AddRegistrationAsync(Provider registered) => throw new NotSupportedException();
        public Task<bool> TryUpdateVerificationStatusAsync(
            Guid providerId,
            ProviderVerificationStatus expected,
            ProviderVerificationStatus next) => throw new NotSupportedException();
    }

    private sealed class FakeClinicRepository(bool exists) : IClinicRepository
    {
        public Task<bool> ClinicExistsAsync(Guid clinicId) => Task.FromResult(exists);
        public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) => throw new NotSupportedException();
        public Task<IEnumerable<Clinic>> GetAllClinicsAsync() => throw new NotSupportedException();
        public Task<Guid> AddClinicAsync(Clinic clinic) => throw new NotSupportedException();
        public Task<Guid> UpdateClinicAsync(Clinic clinic) => throw new NotSupportedException();
        public Task<Guid> DeleteClinicAsync(Guid clinicId) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(
            PageQuery query,
            int offset) => throw new NotSupportedException();
    }
}
