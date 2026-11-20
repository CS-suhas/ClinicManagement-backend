using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Appointments.Services;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class AppointmentServiceTests
{
    private static readonly DateTimeOffset CurrentTime = new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAppointmentAsync_PersistsRequestWithInjectedTime()
    {
        var repository = new FakeAppointmentRepository();
        var service = CreateService(repository, new FakePatientRepository { Exists = true });
        var request = CreateRequest(CurrentTime.AddMinutes(20));

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

    private static AppointmentService CreateService(
        FakeAppointmentRepository appointmentRepository,
        FakePatientRepository patientRepository) =>
        new(
            appointmentRepository,
            patientRepository,
            new ClinicManagementSettings { DefaultAppointmentDurationMinutes = 20 },
            new FixedTimeProvider(CurrentTime));

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
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public List<Appointment> ProviderAppointments { get; } = [];
        public Appointment? AppointmentById { get; init; }
        public Appointment? AddedAppointment { get; private set; }
        public Appointment? UpdatedAppointment { get; private set; }
        public int ProviderAppointmentLookupCount { get; private set; }

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
    }
}
