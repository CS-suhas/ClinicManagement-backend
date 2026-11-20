using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Providers.Services;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class ProviderServiceTests
{
    [Fact]
    public async Task AddProviderAsync_PersistsProviderForExistingClinic()
    {
        var clinicId = Guid.NewGuid();
        var providerRepository = new FakeProviderRepository();
        var clinicRepository = new FakeClinicRepository { Exists = true };
        var service = new ProviderService(providerRepository, clinicRepository);
        var request = new ProviderRequest(Guid.NewGuid(), clinicId, "  Grace ", " Hopper  ", "  Computing ");

        var result = await service.AddProviderAsync(request);

        Assert.Equal(request.Id, result);
        Assert.NotNull(providerRepository.AddedProvider);
        Assert.Equal(clinicId, providerRepository.AddedClinicId);
        Assert.Equal(clinicId, providerRepository.AddedProvider.ClinicId);
        Assert.Equal("Grace", providerRepository.AddedProvider.FirstName);
        Assert.Equal("Hopper", providerRepository.AddedProvider.LastName);
        Assert.Equal("Computing", providerRepository.AddedProvider.Specialty);
    }

    [Fact]
    public async Task AddProviderAsync_ThrowsWhenClinicDoesNotExist()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(providerRepository, new FakeClinicRepository { Exists = false });

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.AddProviderAsync(
                new ProviderRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing")));

        Assert.Null(providerRepository.AddedProvider);
    }

    [Fact]
    public async Task DeleteProviderAsync_ThrowsWhenProviderHasAppointments()
    {
        var provider = Provider.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "Computing");
        var providerRepository = new FakeProviderRepository
        {
            ExistingProvider = provider,
            HasAppointments = true
        };
        var service = new ProviderService(providerRepository, new FakeClinicRepository());

        await Assert.ThrowsAsync<ProviderHasAppointmentsException>(
            () => service.DeleteProviderAsync(provider.Id));

        Assert.Null(providerRepository.DeletedProviderId);
    }

    private sealed class FakeProviderRepository : IProviderRepository
    {
        public Provider? AddedProvider { get; private set; }
        public Guid? AddedClinicId { get; private set; }
        public Provider? ExistingProvider { get; init; }
        public bool HasAppointments { get; init; }
        public Guid? DeletedProviderId { get; private set; }

        public Task<Provider?> GetProviderByIdAsync(Guid providerId) =>
            Task.FromResult(ExistingProvider?.Id == providerId ? ExistingProvider : null);
        public Task<IEnumerable<Provider>> GetAllProvidersAsync() =>
            Task.FromResult<IEnumerable<Provider>>(Array.Empty<Provider>());
        public Task<Guid> AddProviderAsync(Provider provider, Guid clinicId)
        {
            AddedProvider = provider;
            AddedClinicId = clinicId;
            return Task.FromResult(provider.Id);
        }
        public Task<Guid> UpdateProviderAsync(Provider provider, Guid clinicId) => Task.FromResult(provider.Id);
        public Task<Guid> DeleteProviderAsync(Guid providerId)
        {
            DeletedProviderId = providerId;
            return Task.FromResult(providerId);
        }
        public Task<bool> HasAppointmentsAsync(Guid providerId) => Task.FromResult(HasAppointments);
    }

    private sealed class FakeClinicRepository : IClinicRepository
    {
        public bool Exists { get; init; }

        public Task<bool> ClinicExistsAsync(Guid clinicId) => Task.FromResult(Exists);
        public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) => Task.FromResult<Clinic?>(null);
        public Task<IEnumerable<Clinic>> GetAllClinicsAsync() =>
            Task.FromResult<IEnumerable<Clinic>>(Array.Empty<Clinic>());
        public Task<Guid> AddClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> UpdateClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> DeleteClinicAsync(Guid clinicId) => Task.FromResult(clinicId);
    }
}
