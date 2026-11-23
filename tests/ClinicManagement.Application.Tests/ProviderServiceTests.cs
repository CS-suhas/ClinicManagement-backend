using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Providers.Services;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class ProviderServiceTests
{
    [Fact]
    public async Task RegisterProviderAsync_StoresAPendingProviderWithoutAClinic()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository());
        var id = Guid.NewGuid();

        var result = await service.RegisterProviderAsync(new ProviderRegistrationRequest(id, " Grace ", "Hopper", "Computing"));

        Assert.Equal(id, result);
        var registered = Assert.IsType<Provider>(providerRepository.Registered);
        Assert.Equal(ProviderVerificationStatus.PENDING, registered.VerificationStatus);
        Assert.Null(registered.ClinicId);
        Assert.Equal("Grace", registered.FirstName);
    }

    [Fact]
    public async Task RegisterProviderAsync_RejectsBlankSpecialtyBeforeStoring()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository());

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.RegisterProviderAsync(new ProviderRegistrationRequest(Guid.NewGuid(), "Grace", "Hopper", " ")));

        Assert.Equal("specialty", exception.PropertyName);
        Assert.Null(providerRepository.Registered);
    }

    [Fact]
    public async Task VerifyProviderAsync_MovesPendingProviderToVerified()
    {
        var pending = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var providerRepository = new FakeProviderRepository { ExistingProvider = pending };
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository());

        await service.VerifyProviderAsync(pending.Id);

        Assert.Equal(
            (ProviderVerificationStatus.PENDING, ProviderVerificationStatus.VERIFIED),
            providerRepository.VerificationChange);
    }

    [Fact]
    public async Task RejectProviderAsync_ThrowsWhenProviderIsAlreadyVerified()
    {
        var verified = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var providerRepository = new FakeProviderRepository { ExistingProvider = verified };
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository());

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => service.RejectProviderAsync(verified.Id));

        Assert.Null(providerRepository.VerificationChange);
    }

    [Fact]
    public async Task VerifyProviderAsync_ThrowsConflictWhenStatusChangedConcurrently()
    {
        var pending = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var providerRepository = new FakeProviderRepository
        {
            ExistingProvider = pending,
            VerificationChangeSucceeds = false
        };
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository());

        await Assert.ThrowsAsync<ConcurrentModificationException>(() => service.VerifyProviderAsync(pending.Id));
    }

    [Fact]
    public async Task GetProviderCalendarAsync_RejectsInvertedAndOversizedRanges()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository());
        var from = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var inverted = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetProviderCalendarAsync(provider.Id, from, from.AddDays(-1), false));
        var oversized = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetProviderCalendarAsync(provider.Id, from, from.AddDays(63), false));

        Assert.Equal("from", inverted.PropertyName);
        Assert.Equal("to", oversized.PropertyName);
    }

    [Fact]
    public async Task GetProviderCalendarAsync_ThrowsNotFoundForUnknownProvider()
    {
        var service = new ProviderService(
            new FakeProviderRepository(),
            new FakeClinicRepository(),
            new FakeAppointmentRepository());
        var from = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetProviderCalendarAsync(Guid.NewGuid(), from, from.AddDays(7), false));
    }

    [Fact]
    public async Task GetProviderByIdAsync_ReturnsFirstAndLastNameSeparately()
    {
        var provider = Provider.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository());

        var result = await service.GetProviderByIdAsync(provider.Id);

        Assert.Equal("Grace", result.FirstName);
        Assert.Equal("Hopper", result.LastName);
    }

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
        public Task<Provider?> GetByIdentitySubjectAsync(string identitySubject) => Task.FromResult<Provider?>(null);
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId) => throw new NotSupportedException();
        public Provider? Registered { get; private set; }
        public bool VerificationChangeSucceeds { get; init; } = true;
        public (ProviderVerificationStatus Expected, ProviderVerificationStatus Next)? VerificationChange
        {
            get; private set;
        }

        public Task<Guid> AddRegistrationAsync(Provider provider)
        {
            Registered = provider;
            return Task.FromResult(provider.Id);
        }
        public Task<bool> TryUpdateVerificationStatusAsync(
            Guid providerId,
            ProviderVerificationStatus expected,
            ProviderVerificationStatus next)
        {
            if (VerificationChangeSucceeds) VerificationChange = (expected, next);
            return Task.FromResult(VerificationChangeSucceeds);
        }
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
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus? verificationStatus = null) =>
            throw new NotSupportedException();
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
        public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset) => throw new NotSupportedException();
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public Task<bool> TryUpdateStatusAsync(
            Guid appointmentId,
            ClinicManagement.Domain.Enums.AppointmentStatus expected,
            ClinicManagement.Domain.Enums.AppointmentStatus next) => throw new NotSupportedException();
        public Task<IReadOnlyList<ClinicManagement.Application.Models.AppointmentListResponse>>
            GetProviderAppointmentsInRangeAsync(
                Guid providerId,
                DateTimeOffset start,
                DateTimeOffset end,
                bool includeInactive) => throw new NotSupportedException();
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
        public Task<(IReadOnlyList<ClinicManagement.Application.Models.AppointmentListResponse> Items, int TotalCount)>
            SearchAppointmentsAsync(ClinicManagement.Application.Models.AppointmentSearchQuery query, int offset) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ClinicManagement.Application.Models.AppointmentListResponse>>
            GetUpcomingAppointmentsAsync(DateTimeOffset from, int count) => throw new NotSupportedException();
        public Task<bool> HasProviderAppointmentOverlapAsync(
            Guid providerId,
            DateTimeOffset appointmentStart,
            DateTimeOffset appointmentEnd,
            Guid? excludedAppointmentId) => throw new NotSupportedException();
    }
}
