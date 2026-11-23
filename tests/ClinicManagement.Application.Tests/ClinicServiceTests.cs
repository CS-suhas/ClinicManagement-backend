using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Clinics.Services;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class ClinicServiceTests
{
    [Fact]
    public async Task AddClinicAsync_PersistsClinicAndReturnsItsId()
    {
        var repository = new FakeClinicRepository();
        var service = CreateService(repository);
        var request = new ClinicRequest(Guid.NewGuid(), "  Downtown Clinic ", "  10 Main St  ", " 555-0100 ");

        var result = await service.AddClinicAsync(request);

        Assert.Equal(request.Id, result);
        Assert.NotNull(repository.AddedClinic);
        Assert.Equal("Downtown Clinic", repository.AddedClinic.Name);
        Assert.Equal("10 Main St", repository.AddedClinic.Address);
        Assert.Equal("555-0100", repository.AddedClinic.PhoneNumber);
    }

    [Fact]
    public async Task AddClinicAsync_AllowsMissingPhoneNumber()
    {
        var repository = new FakeClinicRepository();
        var service = CreateService(repository);
        var request = new ClinicRequest(Guid.NewGuid(), "Downtown Clinic", null);

        await service.AddClinicAsync(request);

        Assert.NotNull(repository.AddedClinic);
        Assert.Null(repository.AddedClinic.PhoneNumber);
    }

    [Fact]
    public async Task UpdateClinicAsync_AllowsMissingPhoneNumber()
    {
        var clinicId = Guid.NewGuid();
        var repository = new FakeClinicRepository
        {
            ExistingClinic = Clinic.Create(clinicId, "Downtown Clinic", null, "555-0100")
        };
        var service = CreateService(repository);

        await service.UpdateClinicAsync(new ClinicRequest(clinicId, "Downtown Clinic", null));

        Assert.NotNull(repository.UpdatedClinic);
        Assert.Null(repository.UpdatedClinic.PhoneNumber);
    }

    [Fact]
    public async Task DeleteClinicAsync_ThrowsWhenClinicDoesNotExist()
    {
        var repository = new FakeClinicRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.DeleteClinicAsync(Guid.NewGuid()));

        Assert.Null(repository.DeletedClinicId);
    }

    [Fact]
    public async Task GetProvidersByClinicIdAsync_ReturnsClinicProviders()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var provider = Provider.Create(Guid.NewGuid(), "Grace", "Hopper", "Computing", clinic);
        var providers = new FakeProviderRepository();
        providers.Providers.Add(provider);
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            providers);

        var result = await service.GetProvidersByClinicIdAsync(clinic.Id);

        var response = Assert.Single(result);
        Assert.Equal(provider.Id, response.Id);
        Assert.Equal(clinic.Id, response.ClinicId);
    }

    [Fact]
    public async Task GetAppointmentsByClinicIdAsync_ReturnsAppointmentsForClinicProviders()
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
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            new FakeProviderRepository(),
            appointments);

        var result = await service.GetAppointmentsByClinicIdAsync(clinic.Id);

        var response = Assert.Single(result);
        Assert.Equal(appointment.Id, response.Id);
        Assert.Equal(provider.Id, response.ProviderId);
    }

    [Fact]
    public async Task GetProvidersByClinicIdAsync_ThrowsWhenClinicDoesNotExist()
    {
        var service = CreateService(new FakeClinicRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetProvidersByClinicIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddClinicAdminAsync_StoresAdminForExistingClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Downtown Clinic", "10 Main St", "555-0100");
        var adminRepository = new FakeClinicAdminRepository();
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            clinicAdminRepository: adminRepository);

        var id = await service.AddClinicAdminAsync(clinic.Id, " subject-123 ");

        Assert.NotNull(adminRepository.Added);
        Assert.Equal(id, adminRepository.Added.Id);
        Assert.Equal(clinic.Id, adminRepository.Added.ClinicId);
        Assert.Equal("subject-123", adminRepository.Added.IdentitySubject);
    }

    [Fact]
    public async Task AddClinicAdminAsync_ThrowsNotFoundForUnknownClinic()
    {
        var adminRepository = new FakeClinicAdminRepository();
        var service = CreateService(new FakeClinicRepository(), clinicAdminRepository: adminRepository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.AddClinicAdminAsync(Guid.NewGuid(), "subject-123"));

        Assert.Null(adminRepository.Added);
    }

    [Fact]
    public async Task AddClinicAdminAsync_ThrowsConflictWhenIdentityAlreadyAdministersClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Downtown Clinic", "10 Main St", "555-0100");
        var adminRepository = new FakeClinicAdminRepository { AlreadyAdmin = true };
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            clinicAdminRepository: adminRepository);

        await Assert.ThrowsAsync<ClinicAdminAlreadyAssignedException>(
            () => service.AddClinicAdminAsync(clinic.Id, "subject-123"));

        Assert.Null(adminRepository.Added);
    }

    [Fact]
    public async Task GetAppointmentsByClinicIdAsync_ForbidsClinicAdminOfAnotherClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            currentUser: new TestCurrentUser { AdministeredClinicIds = [Guid.NewGuid()] });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAppointmentsByClinicIdAsync(clinic.Id));
    }

    [Fact]
    public async Task GetAppointmentsByClinicIdAsync_AllowsClinicAdminOfThatClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            currentUser: new TestCurrentUser { AdministeredClinicIds = [clinic.Id] });

        var result = await service.GetAppointmentsByClinicIdAsync(clinic.Id);

        Assert.Empty(result);
    }

    private static ClinicService CreateService(
        FakeClinicRepository clinicRepository,
        FakeProviderRepository? providerRepository = null,
        FakeAppointmentRepository? appointmentRepository = null,
        FakeClinicAdminRepository? clinicAdminRepository = null,
        TestCurrentUser? currentUser = null) =>
        new(
            clinicRepository,
            providerRepository ?? new FakeProviderRepository(),
            appointmentRepository ?? new FakeAppointmentRepository(),
            clinicAdminRepository ?? new FakeClinicAdminRepository(),
            currentUser ?? new TestCurrentUser { Roles = ["admin"] });

    private sealed class FakeClinicAdminRepository : IClinicAdminRepository
    {
        public bool AlreadyAdmin { get; init; }
        public ClinicAdmin? Added { get; private set; }

        public Task<bool> IsAdminOfClinicAsync(string identitySubject, Guid clinicId) =>
            Task.FromResult(AlreadyAdmin);

        public Task<bool> IsAnyAdminAsync(string identitySubject) => Task.FromResult(AlreadyAdmin);

        public Task<IReadOnlyList<Guid>> GetClinicIdsAsync(string identitySubject) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task AddAsync(ClinicAdmin clinicAdmin)
        {
            Added = clinicAdmin;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClinicRepository : IClinicRepository
    {
        public Clinic? AddedClinic { get; private set; }
        public Clinic? ExistingClinic { get; init; }
        public Clinic? UpdatedClinic { get; private set; }
        public Guid? DeletedClinicId { get; private set; }

        public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId ? ExistingClinic : null);
        public Task<IEnumerable<Clinic>> GetAllClinicsAsync() =>
            Task.FromResult<IEnumerable<Clinic>>(Array.Empty<Clinic>());
        public Task<Guid> AddClinicAsync(Clinic clinic)
        {
            AddedClinic = clinic;
            return Task.FromResult(clinic.Id);
        }
        public Task<Guid> UpdateClinicAsync(Clinic clinic)
        {
            UpdatedClinic = clinic;
            return Task.FromResult(clinic.Id);
        }
        public Task<Guid> DeleteClinicAsync(Guid clinicId)
        {
            DeletedClinicId = clinicId;
            return Task.FromResult(clinicId);
        }
        public Task<bool> ClinicExistsAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId);
        public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset) => throw new NotSupportedException();
    }

    private sealed class FakeProviderRepository : IProviderRepository
    {
        public Task<Provider?> GetByIdentitySubjectAsync(string identitySubject) => Task.FromResult<Provider?>(null);
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId) => throw new NotSupportedException();
        public Task<Guid> AddRegistrationAsync(Provider provider) => throw new NotSupportedException();
        public Task<bool> TryUpdateVerificationStatusAsync(
            Guid providerId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus expected,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus next) => throw new NotSupportedException();
        public List<Provider> Providers { get; } = [];

        public Task<Provider?> GetProviderByIdAsync(Guid providerId) => Task.FromResult<Provider?>(null);
        public Task<IEnumerable<Provider>> GetAllProvidersAsync() =>
            Task.FromResult<IEnumerable<Provider>>(Providers);
        public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
            Task.FromResult<IEnumerable<Provider>>(Providers.Where(provider => provider.ClinicId == clinicId));
        public Task<Guid> AddProviderAsync(Provider provider, Guid clinicId) => Task.FromResult(provider.Id);
        public Task<Guid> UpdateProviderAsync(Provider provider, Guid clinicId) => Task.FromResult(provider.Id);
        public Task<Guid> DeleteProviderAsync(Guid providerId) => Task.FromResult(providerId);
        public Task<bool> HasAppointmentsAsync(Guid providerId) => Task.FromResult(false);
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus? verificationStatus = null) =>
            throw new NotSupportedException();
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
