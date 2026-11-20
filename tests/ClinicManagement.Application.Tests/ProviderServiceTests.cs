using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Providers.Services;
using ClinicManagement.Application.Appointments.Interfaces;
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
        var service = new ProviderService(providerRepository, clinicRepository, new FakeAppointmentRepository());
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
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository { Exists = false },
            new FakeAppointmentRepository());

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
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository());

        await Assert.ThrowsAsync<ProviderHasAppointmentsException>(
            () => service.DeleteProviderAsync(provider.Id));

        Assert.Null(providerRepository.DeletedProviderId);
    }

    [Fact]
    public async Task GetClinicByProviderIdAsync_ReturnsTheProvidersClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", "Address", "555-0100");
        var provider = Provider.Create(Guid.NewGuid(), "Grace", "Hopper", "Computing", clinic);
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository { ExistingClinic = clinic },
            new FakeAppointmentRepository());

        var result = await service.GetClinicByProviderIdAsync(provider.Id);

        Assert.Equal(clinic.Id, result.Id);
    }

    [Fact]
    public async Task GetAppointmentsByProviderIdAsync_ReturnsProviderAppointments()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var provider = Provider.Create(Guid.NewGuid(), "Grace", "Hopper", "Computing", clinic);
        var patient = Patient.Create(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            "ada@example.com",
            "555-0100");
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(1),
            "Checkup",
            patient,
            provider,
            DateTimeOffset.UtcNow);
        var appointments = new FakeAppointmentRepository();
        appointments.Appointments.Add(appointment);
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository { ExistingClinic = clinic },
            appointments);

        var result = await service.GetAppointmentsByProviderIdAsync(provider.Id);

        var response = Assert.Single(result);
        Assert.Equal(appointment.Id, response.Id);
        Assert.Equal(provider.Id, response.ProviderId);
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
        public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
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
        public Clinic? ExistingClinic { get; init; }

        public Task<bool> ClinicExistsAsync(Guid clinicId) => Task.FromResult(Exists);
        public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId ? ExistingClinic : null);
        public Task<IEnumerable<Clinic>> GetAllClinicsAsync() =>
            Task.FromResult<IEnumerable<Clinic>>(Array.Empty<Clinic>());
        public Task<Guid> AddClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> UpdateClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> DeleteClinicAsync(Guid clinicId) => Task.FromResult(clinicId);
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public List<Appointment> Appointments { get; } = [];

        public Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId) =>
            Task.FromResult<Appointment?>(null);
        public Task<IEnumerable<Appointment>> GetAllAppointmentsAsync() =>
            Task.FromResult<IEnumerable<Appointment>>(Appointments);
        public Task<Guid> AddAppointmentAsync(Appointment appointment, Guid patientId, Guid providerId) =>
            Task.FromResult(appointment.Id);
        public Task<Guid> UpdateAppointmentAsync(Appointment appointment, Guid patientId, Guid providerId) =>
            Task.FromResult(appointment.Id);
        public Task<Guid> DeleteAppointmentAsync(Guid appointmentId) => Task.FromResult(appointmentId);
        public Task<IEnumerable<Appointment>> GetAppointmentsByProviderIdAsync(Guid providerId) =>
            Task.FromResult<IEnumerable<Appointment>>(Appointments.Where(a => a.ProviderId == providerId));
        public Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(Guid patientId) =>
            Task.FromResult<IEnumerable<Appointment>>(Appointments.Where(a => a.PatientId == patientId));
        public Task<IEnumerable<Appointment>> GetAppointmentsByClinicIdAsync(Guid clinicId) =>
            Task.FromResult<IEnumerable<Appointment>>(Appointments.Where(a => a.Provider.ClinicId == clinicId));
    }
}
