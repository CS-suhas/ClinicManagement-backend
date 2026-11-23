using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Appointments.Services;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;
using AppointmentSearchQuery = ClinicManagement.Application.Models.AppointmentSearchQuery;

namespace ClinicManagement.Application.Tests;

public class AppointmentServiceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAppointmentAsync_CreatesRequestedAppointment()
    {
        var repository = new FakeAppointmentRepository();
        var service = CreateService(repository, new FakePatientRepository { Exists = true });

        await service.AddAppointmentAsync(CreateRequest(CurrentTime.AddMinutes(20)));

        Assert.Equal(AppointmentStatus.REQUESTED, repository.AddedAppointment?.Status);
    }

    [Fact]
    public async Task AddAppointmentAsync_RejectsProviderThatIsNotVerified()
    {
        var repository = new FakeAppointmentRepository();
        var pending = Provider.Register(Guid.NewGuid(), "New", "Doctor", "General");
        var service = CreateService(
            repository,
            new FakePatientRepository { Exists = true },
            providerRepository: new FakeProviderRepository { ConfiguredProvider = pending });

        await Assert.ThrowsAsync<ProviderNotAcceptingAppointmentsException>(
            () => service.AddAppointmentAsync(
                CreateRequest(CurrentTime.AddMinutes(20)) with { ProviderId = pending.Id }));

        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task AddAppointmentAsync_RejectsVerifiedProviderWithoutAClinic()
    {
        var repository = new FakeAppointmentRepository();
        var unassigned = Provider.Register(Guid.NewGuid(), "New", "Doctor", "General");
        unassigned.Verify();
        var service = CreateService(
            repository,
            new FakePatientRepository { Exists = true },
            providerRepository: new FakeProviderRepository { ConfiguredProvider = unassigned });

        await Assert.ThrowsAsync<ProviderNotAcceptingAppointmentsException>(
            () => service.AddAppointmentAsync(
                CreateRequest(CurrentTime.AddMinutes(20)) with { ProviderId = unassigned.Id }));

        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task AddAppointmentAsync_ThrowsNotFoundForUnknownProvider()
    {
        var repository = new FakeAppointmentRepository();
        var service = CreateService(
            repository,
            new FakePatientRepository { Exists = true },
            providerRepository: new FakeProviderRepository { ProviderMissing = true });

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.AddAppointmentAsync(CreateRequest(CurrentTime.AddMinutes(20))));

        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task ConfirmAppointmentAsync_MovesRequestedAppointmentToConfirmed()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(
                request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: ProviderUser(request.ProviderId));

        await service.ConfirmAppointmentAsync(request.Id);

        Assert.Equal((AppointmentStatus.REQUESTED, AppointmentStatus.CONFIRMED), repository.StatusChange);
    }

    [Fact]
    public async Task CancelAppointmentAsync_UsesTheStoredStatusAsTheExpectedOne()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var appointment = CreateExistingAppointment(
            request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id);
        appointment.Confirm(CurrentTime);
        var repository = new FakeAppointmentRepository { AppointmentById = appointment };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: PatientUser(request.PatientId));

        await service.CancelAppointmentAsync(request.Id);

        Assert.Equal((AppointmentStatus.CONFIRMED, AppointmentStatus.CANCELLED), repository.StatusChange);
    }

    [Fact]
    public async Task CompleteAppointmentAsync_RejectsAppointmentThatWasNeverConfirmed()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(
                request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: ProviderUser(request.ProviderId));

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => service.CompleteAppointmentAsync(request.Id));

        Assert.Null(repository.StatusChange);
    }

    [Fact]
    public async Task ConfirmAppointmentAsync_ThrowsConflictWhenStatusChangedConcurrently()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            StatusChangeSucceeds = false,
            AppointmentById = CreateExistingAppointment(
                request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: ProviderUser(request.ProviderId));

        await Assert.ThrowsAsync<ConcurrentModificationException>(
            () => service.ConfirmAppointmentAsync(request.Id));
    }

    [Fact]
    public async Task DeclineAppointmentAsync_ThrowsNotFoundForUnknownAppointment()
    {
        var service = CreateService(new FakeAppointmentRepository(), new FakePatientRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.DeclineAppointmentAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ConfirmAppointmentAsync_ForbidsProviderThatDoesNotOwnAppointment()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(
                request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: ProviderUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ConfirmAppointmentAsync(request.Id));

        Assert.Null(repository.StatusChange);
    }

    [Fact]
    public async Task DeclineAppointmentAsync_ForbidsUnverifiedProvider()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(
                request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var pending = Provider.Register(request.ProviderId, "Grace", "Hopper", "Computing");
        var service = CreateService(repository, new FakePatientRepository(), currentUser: new TestCurrentUser { Provider = pending });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeclineAppointmentAsync(request.Id));

        Assert.Null(repository.StatusChange);
    }

    [Fact]
    public async Task AddAppointmentAsync_PersistsRequestWithInjectedTime()
    {
        var repository = new FakeAppointmentRepository();
        var request = CreateRequest(CurrentTime.AddMinutes(20));
        var service = CreateService(
            repository,
            new FakePatientRepository { Exists = true },
            currentUser: PatientUser(request.PatientId));

        var result = await service.AddAppointmentAsync(request);

        Assert.Equal(request.Id, result);
        Assert.NotNull(repository.AddedAppointment);
        Assert.Equal(request.Id, repository.AddedAppointment.Id);
        Assert.Equal(request.PatientId, repository.AddedAppointment.PatientId);
        Assert.Equal(request.ProviderId, repository.AddedAppointment.ProviderId);
        Assert.Equal(request.AppointmentDateTime, repository.AddedAppointment.AppointmentDateTime);
    }

    [Fact]
    public async Task AddAppointmentAsync_RejectsAppointmentAtCurrentTime()
    {
        var repository = new FakeAppointmentRepository();
        var patientRepository = new FakePatientRepository { Exists = true };
        var service = CreateService(repository, patientRepository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.AddAppointmentAsync(CreateRequest(CurrentTime)));

        Assert.Equal("appointmentDateTime", exception.PropertyName);
        Assert.Equal(0, patientRepository.PatientLookupCount);
        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task AddAppointmentAsync_RejectsEmptyGuidBeforeRepositoryCalls()
    {
        var repository = new FakeAppointmentRepository();
        var patientRepository = new FakePatientRepository { Exists = true };
        var service = CreateService(repository, patientRepository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.AddAppointmentAsync(CreateRequest(CurrentTime.AddHours(1)) with { ProviderId = Guid.Empty }));

        Assert.Equal("providerId", exception.PropertyName);
        Assert.Equal(0, patientRepository.PatientLookupCount);
        Assert.Equal(0, repository.ProviderAppointmentLookupCount);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(15, 40)]
    [InlineData(23, 50)]
    public async Task AddAppointmentAsync_RejectsAppointmentsOutsideClinicHours(int hour, int minute)
    {
        var repository = new FakeAppointmentRepository();
        var service = CreateService(repository, new FakePatientRepository { Exists = true });
        var appointmentDateTime = new DateTimeOffset(2030, 1, 2, hour, minute, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<AppointmentOutsideClinicHoursException>(
            () => service.AddAppointmentAsync(CreateRequest(appointmentDateTime)));

        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task AddAppointmentAsync_RejectsOverlappingProviderAppointment()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository();
        repository.ProviderAppointments.Add(
            CreateExistingAppointment(request.ProviderId, request.PatientId, request.AppointmentDateTime.AddMinutes(19)));
        var service = CreateService(repository, new FakePatientRepository { Exists = true });

        var exception = await Assert.ThrowsAsync<ProviderAppointmentOverlapException>(
            () => service.AddAppointmentAsync(request));

        Assert.Equal(request.ProviderId, exception.ProviderId);
        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task AddAppointmentAsync_AllowsAppointmentsExactlyDurationApart()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository();
        repository.ProviderAppointments.Add(
            CreateExistingAppointment(request.ProviderId, request.PatientId, request.AppointmentDateTime.AddMinutes(20)));
        var service = CreateService(repository, new FakePatientRepository { Exists = true });

        await service.AddAppointmentAsync(request);

        Assert.NotNull(repository.AddedAppointment);
    }

    [Fact]
    public async Task AddAppointmentAsync_RejectsMissingPatientWithoutSaving()
    {
        var repository = new FakeAppointmentRepository();
        var service = CreateService(repository, new FakePatientRepository { Exists = false });

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.AddAppointmentAsync(CreateRequest(CurrentTime.AddHours(1))));

        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task UpdateAppointmentAsync_ChecksOverlapAgainstRequestedTime()
    {
        var request = CreateRequest(CurrentTime.AddHours(4).AddMinutes(5));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(
                request.ProviderId,
                request.PatientId,
                CurrentTime.AddHours(2),
                request.Id)
        };
        repository.ProviderAppointments.Add(
            CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(4)));
        var service = CreateService(repository, new FakePatientRepository());

        var exception = await Assert.ThrowsAsync<ProviderAppointmentOverlapException>(
            () => service.UpdateAppointmentAsync(request));

        Assert.Equal(request.ProviderId, exception.ProviderId);
        Assert.Null(repository.UpdatedAppointment);
    }

    [Fact]
    public async Task UpdateAppointmentAsync_ChecksOverlapWhenProviderChanges()
    {
        var oldProviderId = Guid.NewGuid();
        var request = CreateRequest(CurrentTime.AddHours(4));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(
                oldProviderId,
                request.PatientId,
                request.AppointmentDateTime,
                request.Id)
        };
        repository.ProviderAppointments.Add(
            CreateExistingAppointment(
                request.ProviderId,
                request.PatientId,
                request.AppointmentDateTime.AddMinutes(10)));
        var service = CreateService(repository, new FakePatientRepository());

        await Assert.ThrowsAsync<ProviderAppointmentOverlapException>(
            () => service.UpdateAppointmentAsync(request));

        Assert.Null(repository.UpdatedAppointment);
    }

    [Fact]
    public async Task UpdateAppointmentAsync_ExcludesAppointmentBeingUpdated()
    {
        var request = CreateRequest(CurrentTime.AddHours(4));
        var existingAppointment = CreateExistingAppointment(
            request.ProviderId,
            request.PatientId,
            CurrentTime.AddHours(2),
            request.Id);
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = existingAppointment
        };
        repository.ProviderAppointments.Add(existingAppointment);
        var service = CreateService(repository, new FakePatientRepository());

        var result = await service.UpdateAppointmentAsync(request);

        Assert.Equal(request.Id, result);
        Assert.NotNull(repository.UpdatedAppointment);
        Assert.Equal(request.AppointmentDateTime, repository.UpdatedAppointment.AppointmentDateTime);
    }

    [Fact]
    public async Task GetRelatedResourcesByAppointmentIdAsync_ReturnsPatientProviderAndClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Test clinic", "Address", "555-0101");
        var patient = Patient.Create(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            "ada@example.com",
            "555-0100");
        var provider = Provider.Create(Guid.NewGuid(), clinic.Id, "Grace", "Hopper", "Computing");
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            CurrentTime.AddHours(2),
            "Checkup",
            patient,
            provider,
            CurrentTime);
        var service = CreateService(
            new FakeAppointmentRepository { AppointmentById = appointment },
            new FakePatientRepository(),
            new FakeClinicRepository { ExistingClinic = clinic },
            currentUser: PatientUser(patient.Id));

        var appointmentPatient = await service.GetPatientByAppointmentIdAsync(appointment.Id);
        var appointmentProvider = await service.GetProviderByAppointmentIdAsync(appointment.Id);
        var appointmentClinic = await service.GetClinicByAppointmentIdAsync(appointment.Id);

        Assert.Equal(patient.Id, appointmentPatient.Id);
        Assert.Equal(provider.Id, appointmentProvider.Id);
        Assert.Equal(clinic.Id, appointmentProvider.ClinicId);
        Assert.Equal(clinic.Id, appointmentClinic.Id);
    }

    [Fact]
    public async Task GetPatientByAppointmentIdAsync_ThrowsWhenAppointmentDoesNotExist()
    {
        var service = CreateService(new FakeAppointmentRepository(), new FakePatientRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetPatientByAppointmentIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAppointmentAsync_RejectsAppointmentEndingAtClinicClosingTime()
    {
        var request = CreateRequest(new DateTimeOffset(2030, 1, 2, 15, 40, 0, TimeSpan.Zero));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(
                request.ProviderId,
                request.PatientId,
                CurrentTime.AddHours(2),
                request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository());

        await Assert.ThrowsAsync<AppointmentOutsideClinicHoursException>(
            () => service.UpdateAppointmentAsync(request));

        Assert.Null(repository.UpdatedAppointment);
    }

    private static readonly Patient DefaultPatient = Patient.Create(
        Guid.NewGuid(),
        "Ada",
        "Lovelace",
        new DateOnly(1985, 12, 10),
        "ada@example.com",
        "555-0100");

    private static TestCurrentUser PatientUser(Guid patientId) =>
        new()
        {
            Patient = Patient.Create(patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100")
        };

    private static TestCurrentUser ProviderUser(Guid providerId) =>
        new() { Provider = Provider.Create(providerId, Guid.NewGuid(), "Grace", "Hopper", "Computing") };

    private static AppointmentService CreateService(
        FakeAppointmentRepository appointmentRepository,
        FakePatientRepository patientRepository,
        FakeClinicRepository? clinicRepository = null,
        FakeProviderRepository? providerRepository = null,
        TestCurrentUser? currentUser = null) =>
        new(
            appointmentRepository,
            patientRepository,
            clinicRepository ?? new FakeClinicRepository(),
            providerRepository ?? new FakeProviderRepository(),
            currentUser ?? new TestCurrentUser { Patient = DefaultPatient },
            new ClinicManagementSettings { DefaultAppointmentDurationMinutes = 20 },
            new FixedTimeProvider(CurrentTime));

    [Fact]
    public async Task AddAppointmentAsync_BooksForSignedInPatientIgnoringRequestPatientId()
    {
        var repository = new FakeAppointmentRepository();
        var callerId = Guid.NewGuid();
        var service = CreateService(
            repository,
            new FakePatientRepository { Exists = true },
            currentUser: PatientUser(callerId));

        await service.AddAppointmentAsync(CreateRequest(CurrentTime.AddMinutes(20)));

        Assert.Equal(callerId, repository.AddedAppointment?.PatientId);
    }

    [Fact]
    public async Task AddAppointmentAsync_RejectsCallerWhoIsNotAPatient()
    {
        var repository = new FakeAppointmentRepository();
        var service = CreateService(
            repository,
            new FakePatientRepository { Exists = true },
            currentUser: new TestCurrentUser());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.AddAppointmentAsync(CreateRequest(CurrentTime.AddMinutes(20))));

        Assert.Null(repository.AddedAppointment);
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_RejectsUnrelatedPatient()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: PatientUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAppointmentByIdAsync(request.Id));
    }

    [Fact]
    public async Task CancelAppointmentAsync_RejectsUnrelatedProvider()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: ProviderUser(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CancelAppointmentAsync(request.Id));

        Assert.Null(repository.StatusChange);
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_AllowsClinicAdminOfTheProvidersClinic()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var appointment = CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id);
        var repository = new FakeAppointmentRepository { AppointmentById = appointment };
        var currentUser = new TestCurrentUser { AdministeredClinicIds = [appointment.Provider.ClinicId!.Value] };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: currentUser);

        var result = await service.GetAppointmentByIdAsync(request.Id);

        Assert.Equal(request.Id, result.Id);
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_ForbidsClinicAdminOfAnotherClinic()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var currentUser = new TestCurrentUser { AdministeredClinicIds = [Guid.NewGuid()] };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAppointmentByIdAsync(request.Id));
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_AllowsAdministratorForAnyAppointment()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: new TestCurrentUser { Roles = ["admin"] });

        var result = await service.GetAppointmentByIdAsync(request.Id);

        Assert.Equal(request.Id, result.Id);
    }

    [Fact]
    public async Task GetAppointmentByIdAsync_RequiresSignedInUser()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var anonymous = new TestCurrentUser { IsAuthenticated = false, Subject = null };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: anonymous);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAppointmentByIdAsync(request.Id));
    }

    [Fact]
    public async Task CancelAppointmentAsync_AllowsTheBookingPatient()
    {
        var request = CreateRequest(CurrentTime.AddHours(2));
        var repository = new FakeAppointmentRepository
        {
            AppointmentById = CreateExistingAppointment(request.ProviderId, request.PatientId, CurrentTime.AddHours(2), request.Id)
        };
        var service = CreateService(repository, new FakePatientRepository(), currentUser: PatientUser(request.PatientId));

        await service.CancelAppointmentAsync(request.Id);

        Assert.NotNull(repository.StatusChange);
    }

    [Fact]
    public async Task SearchAppointmentsAsync_PassesProviderIdsToTheRepository()
    {
        var repository = new FakeAppointmentRepository();
        var service = CreateService(repository, new FakePatientRepository(), currentUser: new TestCurrentUser { Roles = ["admin"] });
        var providerIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        await service.SearchAppointmentsAsync(new AppointmentSearchQuery(ProviderIds: providerIds));

        Assert.Equal(providerIds, repository.SearchedQuery?.ProviderIds);
    }

    [Fact]
    public async Task SearchAppointmentsAsync_ForbidsProviderSearchingOtherProviders()
    {
        var self = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", "Provider", "General");
        var repository = new FakeAppointmentRepository();
        var service = CreateService(
            repository,
            new FakePatientRepository(),
            currentUser: new TestCurrentUser { Roles = ["doctor"], Provider = self });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.SearchAppointmentsAsync(new AppointmentSearchQuery(ProviderIds: [self.Id, Guid.NewGuid()])));

        Assert.Null(repository.SearchedQuery);
    }

    [Fact]
    public async Task SearchAppointmentsAsync_ProviderMayFilterByOwnIdOnly()
    {
        var self = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Test", "Provider", "General");
        var repository = new FakeAppointmentRepository();
        var service = CreateService(
            repository,
            new FakePatientRepository(),
            currentUser: new TestCurrentUser { Roles = ["doctor"], Provider = self });

        await service.SearchAppointmentsAsync(new AppointmentSearchQuery(ProviderIds: [self.Id]));

        Assert.Equal(self.Id, repository.SearchedQuery?.ProviderId);
        Assert.Equal(new[] { self.Id }, repository.SearchedQuery?.ProviderIds ?? Array.Empty<Guid>());
    }

    private static AppointmentRequest CreateRequest(DateTimeOffset appointmentDateTime) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), appointmentDateTime, "Checkup");

    private static Appointment CreateExistingAppointment(
        Guid providerId,
        Guid patientId,
        DateTimeOffset appointmentDateTime,
        Guid? appointmentId = null)
    {
        var patient = Patient.Create(
            patientId,
            "Test",
            "Patient",
            new DateOnly(1980, 1, 1),
            "patient@example.com",
            "555-0100");
        var clinic = Clinic.Create(Guid.NewGuid(), "Test clinic", null, "555-0101");
        var provider = Provider.Create(providerId, "Test", "Provider", "General", clinic);

        return Appointment.Create(
            appointmentId ?? Guid.NewGuid(),
            appointmentDateTime,
            "Checkup",
            patient,
            provider,
            CurrentTime);
    }

    private sealed class FixedTimeProvider(DateTimeOffset currentTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => currentTime;
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        public Task<Patient?> GetByIdentitySubjectAsync(string identitySubject) => Task.FromResult<Patient?>(null);
        public bool Exists { get; init; }
        public int PatientLookupCount { get; private set; }

        public Task<bool> PatientExistsAsync(Guid patientId)
        {
            PatientLookupCount++;
            return Task.FromResult(Exists);
        }
        public Task<Patient?> GetPatientByIdAsync(Guid patientId) => Task.FromResult<Patient?>(null);
        public Task<IEnumerable<Patient>> GetAllPatientsAsync() =>
            Task.FromResult<IEnumerable<Patient>>(Array.Empty<Patient>());
        public Task<Guid> AddPatientAsync(Patient patient) => Task.FromResult(patient.Id);
        public Task<Guid> UpdatePatientAsync(Patient patient) => Task.FromResult(patient.Id);
        public Task<Guid> DeletePatientAsync(Guid patientId) => Task.FromResult(patientId);
        public Task<bool> HasDependentRecordsAsync(Guid patientId) => Task.FromResult(false);
        public Task<IEnumerable<PatientAllergy>> GetAllergiesByPatientIdAsync(Guid patientId) =>
            Task.FromResult<IEnumerable<PatientAllergy>>(Array.Empty<PatientAllergy>());
        public Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset) => throw new NotSupportedException();
        public Task<(IReadOnlyList<PatientAllergy> Items, int TotalCount)> SearchPatientAllergiesAsync(
            Guid patientId,
            ClinicManagement.Application.Models.PageQuery query,
            int offset) => throw new NotSupportedException();
        public Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> GetSelectableAllergyTypesAsync(
            Guid patientId,
            Guid? selectedAllergyId,
            ClinicManagement.Application.Models.PageQuery query,
            int offset) =>
            throw new NotSupportedException();
        public Task<Guid> UpsertPatientAllergyAsync(
            Guid patientId,
            Guid allergyId,
            ClinicManagement.Domain.Enums.AllergySeverity severity,
            string? notes) => throw new NotSupportedException();
        public Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public bool StatusChangeSucceeds { get; init; } = true;
        public (AppointmentStatus Expected, AppointmentStatus Next)? StatusChange { get; private set; }

        public Task<bool> TryUpdateStatusAsync(
            Guid appointmentId,
            AppointmentStatus expected,
            AppointmentStatus next)
        {
            if (StatusChangeSucceeds) StatusChange = (expected, next);
            return Task.FromResult(StatusChangeSucceeds);
        }
        public Task<IReadOnlyList<ClinicManagement.Application.Models.AppointmentListResponse>>
            GetProviderAppointmentsInRangeAsync(
                Guid providerId,
                DateTimeOffset start,
                DateTimeOffset end,
                bool includeInactive) => throw new NotSupportedException();
        public List<Appointment> ProviderAppointments { get; } = [];
        public Appointment? AppointmentById { get; init; }
        public Appointment? AddedAppointment { get; private set; }
        public Appointment? UpdatedAppointment { get; private set; }
        public int ProviderAppointmentLookupCount { get; private set; }
        public int OverlapLookupCount { get; private set; }

        public Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId) =>
            Task.FromResult(AppointmentById?.Id == appointmentId ? AppointmentById : null);

        public Task<IEnumerable<Appointment>> GetAllAppointmentsAsync() =>
            Task.FromResult<IEnumerable<Appointment>>(Array.Empty<Appointment>());

        public Task<Guid> AddAppointmentAsync(Appointment appointment, Guid patientId, Guid providerId)
        {
            AddedAppointment = appointment;
            return Task.FromResult(appointment.Id);
        }

        public Task<Guid> UpdateAppointmentAsync(Appointment appointment, Guid patientId, Guid providerId)
        {
            UpdatedAppointment = appointment;
            return Task.FromResult(appointment.Id);
        }

        public Task<Guid> DeleteAppointmentAsync(Guid appointmentId) => Task.FromResult(appointmentId);

        public Task<IEnumerable<Appointment>> GetAppointmentsByProviderIdAsync(Guid providerId)
        {
            ProviderAppointmentLookupCount++;
            return Task.FromResult<IEnumerable<Appointment>>(ProviderAppointments);
        }

        public Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(Guid patientId) =>
            Task.FromResult<IEnumerable<Appointment>>(Array.Empty<Appointment>());

        public Task<IEnumerable<Appointment>> GetAppointmentsByClinicIdAsync(Guid clinicId) =>
            Task.FromResult<IEnumerable<Appointment>>(Array.Empty<Appointment>());
        public ClinicManagement.Application.Models.AppointmentSearchQuery? SearchedQuery { get; private set; }

        public Task<(IReadOnlyList<ClinicManagement.Application.Models.AppointmentListResponse> Items, int TotalCount)>
            SearchAppointmentsAsync(ClinicManagement.Application.Models.AppointmentSearchQuery query, int offset)
        {
            SearchedQuery = query;
            return Task.FromResult<(IReadOnlyList<ClinicManagement.Application.Models.AppointmentListResponse>, int)>(
                ([], 0));
        }
        public Task<IReadOnlyList<ClinicManagement.Application.Models.AppointmentListResponse>>
            GetUpcomingAppointmentsAsync(DateTimeOffset from, int count) => throw new NotSupportedException();
        public Task<bool> HasProviderAppointmentOverlapAsync(
            Guid providerId,
            DateTimeOffset appointmentStart,
            DateTimeOffset appointmentEnd,
            Guid? excludedAppointmentId)
        {
            OverlapLookupCount++;
            return Task.FromResult(ProviderAppointments.Any(appointment =>
                appointment.ProviderId == providerId
                && appointment.Id != excludedAppointmentId
                && appointment.AppointmentDateTime > appointmentStart
                && appointment.AppointmentDateTime < appointmentEnd));
        }
    }

    /// <summary>Unless configured otherwise, any requested provider exists and accepts appointments.</summary>
    private sealed class FakeProviderRepository : IProviderRepository
    {
        public Task<Provider?> GetByIdentitySubjectAsync(string identitySubject) => Task.FromResult<Provider?>(null);
        public Provider? ConfiguredProvider { get; init; }
        public bool ProviderMissing { get; init; }

        public Task<Provider?> GetProviderByIdAsync(Guid providerId) =>
            Task.FromResult(ProviderMissing
                ? null
                : ConfiguredProvider ?? Provider.Create(providerId, Guid.NewGuid(), "Test", "Provider", "General"));
        public Task<IEnumerable<Provider>> GetAllProvidersAsync() => throw new NotSupportedException();
        public Task<Guid> AddProviderAsync(Provider request, Guid clinicId) => throw new NotSupportedException();
        public Task<Guid> UpdateProviderAsync(Provider request, Guid clinicId) => throw new NotSupportedException();
        public Task<Guid> DeleteProviderAsync(Guid providerId) => throw new NotSupportedException();
        public Task<bool> HasAppointmentsAsync(Guid providerId) => throw new NotSupportedException();
        public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus? verificationStatus = null) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId) => throw new NotSupportedException();
        public Task<Guid> AddRegistrationAsync(Provider provider) => throw new NotSupportedException();
        public Task<bool> TryUpdateVerificationStatusAsync(
            Guid providerId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus expected,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus next) => throw new NotSupportedException();
    }

    private sealed class FakeClinicRepository : IClinicRepository
    {
        public Clinic? ExistingClinic { get; init; }

        public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId ? ExistingClinic : null);
        public Task<IEnumerable<Clinic>> GetAllClinicsAsync() =>
            Task.FromResult<IEnumerable<Clinic>>(Array.Empty<Clinic>());
        public Task<Guid> AddClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> UpdateClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> DeleteClinicAsync(Guid clinicId) => Task.FromResult(clinicId);
        public Task<bool> ClinicExistsAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId);
        public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset) => throw new NotSupportedException();
    }
}
