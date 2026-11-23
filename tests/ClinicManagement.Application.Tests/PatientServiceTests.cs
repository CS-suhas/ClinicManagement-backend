using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Patients.Services;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task GetPatientByIdAsync_ThrowsWhenPatientDoesNotExist()
    {
        var service = CreateService(new FakePatientRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetPatientByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPatientByIdAsync_ReturnsFirstAndLastNameSeparately()
    {
        var patient = Patient.Create(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            "ada@example.com",
            "555-0100");
        var service = CreateService(new FakePatientRepository { ExistingPatient = patient });

        var result = await service.GetPatientByIdAsync(patient.Id);

        Assert.Equal("Ada", result.FirstName);
        Assert.Equal("Lovelace", result.LastName);
    }

    [Fact]
    public async Task GetPatientAllergiesAsync_ReturnsAllergyDetailsForThePatient()
    {
        var patient = Patient.Create(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            "ada@example.com",
            "555-0100");
        var allergyType = AllergyType.Create(Guid.NewGuid(), "Peanuts");
        var repository = new FakePatientRepository { ExistingPatient = patient };
        repository.PatientAllergies.Add(
            PatientAllergy.Create(
                Guid.NewGuid(),
                patient,
                allergyType,
                AllergySeverity.SEVERE,
                "Carries an epinephrine auto-injector"));
        var service = CreateService(repository);

        var result = await service.GetPatientAllergiesAsync(patient.Id);

        var allergy = Assert.Single(result);
        Assert.Equal(allergyType.Id, allergy.AllergyId);
        Assert.Equal("Peanuts", allergy.Name);
        Assert.Equal("SEVERE", allergy.Severity);
        Assert.Equal("Carries an epinephrine auto-injector", allergy.Notes);
    }

    [Fact]
    public async Task GetPatientAllergiesAsync_ReturnsEmptyListForPatientWithoutAllergies()
    {
        var patient = Patient.Create(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            "ada@example.com",
            "555-0100");
        var service = CreateService(new FakePatientRepository { ExistingPatient = patient });

        var result = await service.GetPatientAllergiesAsync(patient.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPatientAllergiesAsync_ThrowsWhenPatientDoesNotExist()
    {
        var service = CreateService(new FakePatientRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetPatientAllergiesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPatientAllergiesAsync_RejectsEmptyPatientIdBeforeRepositoryCalls()
    {
        var repository = new FakePatientRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetPatientAllergiesAsync(Guid.Empty));

        Assert.Equal(0, repository.PatientLookupCount);
    }

    [Fact]
    public async Task AddPatientAsync_PersistsTrimmedPatientAndReturnsItsId()
    {
        var repository = new FakePatientRepository();
        var service = CreateService(repository);
        var request = new PatientRequest(
            Guid.NewGuid(),
            "  Ada ",
            " Lovelace  ",
            new DateOnly(1985, 12, 10),
            " 555-0100 ",
            " ada@example.com ");

        var result = await service.AddPatientAsync(request);

        Assert.Equal(request.Id, result);
        Assert.NotNull(repository.AddedPatient);
        Assert.Equal("Ada", repository.AddedPatient.FirstName);
        Assert.Equal("Lovelace", repository.AddedPatient.LastName);
        Assert.Equal("555-0100", repository.AddedPatient.PhoneNumber);
        Assert.Equal("ada@example.com", repository.AddedPatient.Email);
    }

    [Fact]
    public async Task UpdatePatientAsync_ThrowsWhenPatientDoesNotExist()
    {
        var patientId = Guid.NewGuid();
        var repository = new FakePatientRepository();
        var service = CreateService(repository, SignedInPatient(patientId));

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.UpdatePatientAsync(CreatePatientRequest(patientId)));

        Assert.Null(repository.UpdatedPatient);
    }

    [Fact]
    public async Task UpdatePatientAsync_RejectsInvalidEmailBeforeLookingUpPatient()
    {
        var repository = new FakePatientRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.UpdatePatientAsync(CreatePatientRequest(Guid.NewGuid()) with { Email = "not-an-email" }));

        Assert.Equal("email", exception.PropertyName);
        Assert.Equal(0, repository.PatientLookupCount);
    }

    [Fact]
    public async Task DeletePatientAsync_ThrowsWhenPatientHasDependentRecords()
    {
        var patient = Patient.Create(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            "ada@example.com",
            "555-0100");
        var repository = new FakePatientRepository
        {
            ExistingPatient = patient,
            HasDependentRecords = true
        };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<PatientHasRecordsException>(
            () => service.DeletePatientAsync(patient.Id));

        Assert.Null(repository.DeletedPatientId);
    }

    [Fact]
    public async Task GetAppointmentsByPatientIdAsync_ReturnsPatientAppointments()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var patient = Patient.Create(
            Guid.NewGuid(),
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            "ada@example.com",
            "555-0100");
        var provider = Provider.Create(Guid.NewGuid(), "Grace", "Hopper", "Computing", clinic);
        var appointment = Appointment.Create(
            Guid.NewGuid(),
            CurrentTime.AddHours(1),
            "Checkup",
            patient,
            provider,
            CurrentTime);
        var appointmentRepository = new FakeAppointmentRepository();
        appointmentRepository.Appointments.Add(appointment);
        var service = new PatientService(
            new FakePatientRepository { ExistingPatient = patient },
            appointmentRepository,
            new FakeAllergyTypeRepository(),
            new TestCurrentUser { Roles = ["admin"] });

        var result = await service.GetAppointmentsByPatientIdAsync(patient.Id);

        var response = Assert.Single(result);
        Assert.Equal(appointment.Id, response.Id);
        Assert.Equal(patient.Id, response.PatientId);
        Assert.Equal(provider.Id, response.ProviderId);
    }

    [Fact]
    public async Task GetPatientByIdAsync_RejectsAnotherPatient()
    {
        var other = Patient.Create(Guid.NewGuid(), "Grace", "Hopper", new DateOnly(1906, 12, 9), "grace@example.com", "555-0102");
        var service = CreateService(
            new FakePatientRepository { ExistingPatient = other },
            SignedInPatient(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetPatientByIdAsync(other.Id));
    }

    [Fact]
    public async Task GetPatientByIdAsync_AllowsTheSignedInPatient()
    {
        var self = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        var service = CreateService(
            new FakePatientRepository { ExistingPatient = self },
            SignedInPatient(self.Id));

        var result = await service.GetPatientByIdAsync(self.Id);

        Assert.Equal(self.Id, result.Id);
    }

    [Fact]
    public async Task GetAppointmentsByPatientIdAsync_RejectsAnotherPatient()
    {
        var service = CreateService(new FakePatientRepository(), SignedInPatient(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAppointmentsByPatientIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdatePatientAsync_RejectsAnotherPatientWithoutSaving()
    {
        var existing = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        var repository = new FakePatientRepository { ExistingPatient = existing };
        var service = CreateService(repository, SignedInPatient(Guid.NewGuid()));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdatePatientAsync(CreatePatientRequest(existing.Id)));

        Assert.Null(repository.UpdatedPatient);
    }

    private static PatientRequest CreatePatientRequest(Guid id) =>
        new(id, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com");

    private static readonly DateTimeOffset CurrentTime =
        new(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static TestCurrentUser SignedInPatient(Guid patientId) =>
        new()
        {
            Patient = Patient.Create(patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100")
        };

    private static PatientService CreateService(FakePatientRepository repository, TestCurrentUser? currentUser = null) =>
        new(repository, new FakeAppointmentRepository(), new FakeAllergyTypeRepository(), currentUser ?? new TestCurrentUser { Roles = ["admin"] });

    private sealed class FakePatientRepository : IPatientRepository
    {
        public Task<Patient?> GetByIdentitySubjectAsync(string identitySubject) => Task.FromResult<Patient?>(null);
        public Patient? AddedPatient { get; private set; }
        public Patient? UpdatedPatient { get; private set; }
        public Patient? ExistingPatient { get; init; }
        public bool HasDependentRecords { get; init; }
        public Guid? DeletedPatientId { get; private set; }
        public int PatientLookupCount { get; private set; }
        public List<PatientAllergy> PatientAllergies { get; } = [];

        public Task<Patient?> GetPatientByIdAsync(Guid patientId)
        {
            PatientLookupCount++;
            return Task.FromResult(ExistingPatient?.Id == patientId ? ExistingPatient : null);
        }
        public Task<IEnumerable<Patient>> GetAllPatientsAsync() =>
            Task.FromResult<IEnumerable<Patient>>(Array.Empty<Patient>());
        public Task<Guid> AddPatientAsync(Patient patient)
        {
            AddedPatient = patient;
            return Task.FromResult(patient.Id);
        }
        public Task<Guid> UpdatePatientAsync(Patient patient)
        {
            UpdatedPatient = patient;
            return Task.FromResult(patient.Id);
        }
        public Task<Guid> DeletePatientAsync(Guid patientId)
        {
            DeletedPatientId = patientId;
            return Task.FromResult(patientId);
        }
        public Task<bool> PatientExistsAsync(Guid patientId) => Task.FromResult(false);
        public Task<bool> HasDependentRecordsAsync(Guid patientId) => Task.FromResult(HasDependentRecords);
        public Task<IEnumerable<PatientAllergy>> GetAllergiesByPatientIdAsync(Guid patientId) =>
            Task.FromResult<IEnumerable<PatientAllergy>>(PatientAllergies);
        public Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(PageQuery query, int offset) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<PatientAllergy> Items, int TotalCount)> SearchPatientAllergiesAsync(
            Guid patientId,
            PageQuery query,
            int offset) => throw new NotSupportedException();
        public Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> GetSelectableAllergyTypesAsync(
            Guid patientId,
            Guid? selectedAllergyId,
            PageQuery query,
            int offset) =>
            throw new NotSupportedException();
        public Task<Guid> UpsertPatientAllergyAsync(
            Guid patientId,
            Guid allergyId,
            AllergySeverity severity,
            string? notes) => throw new NotSupportedException();
        public Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId) =>
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
        public Task<(IReadOnlyList<AppointmentListResponse> Items, int TotalCount)> SearchAppointmentsAsync(
            AppointmentSearchQuery query,
            int offset) => throw new NotSupportedException();
        public Task<IReadOnlyList<AppointmentListResponse>> GetUpcomingAppointmentsAsync(
            DateTimeOffset from,
            int count) => throw new NotSupportedException();
        public Task<bool> HasProviderAppointmentOverlapAsync(
            Guid providerId,
            DateTimeOffset appointmentStart,
            DateTimeOffset appointmentEnd,
            Guid? excludedAppointmentId) => throw new NotSupportedException();
    }

    private sealed class FakeAllergyTypeRepository : IAllergyTypeRepository
    {
        public Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId) => Task.FromResult<AllergyType?>(null);
        public Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync() =>
            Task.FromResult<IEnumerable<AllergyType>>(Array.Empty<AllergyType>());
        public Task<IEnumerable<Patient>> GetPatientsByAllergyTypeIdAsync(Guid allergyTypeId) =>
            Task.FromResult<IEnumerable<Patient>>(Array.Empty<Patient>());
        public Task<bool> HasPatientsWithAllergyTypeAsync(Guid allergyTypeId) => Task.FromResult(false);
        public Task<Guid> AddAllergyTypeAsync(AllergyType request) => Task.FromResult(request.Id);
        public Task<Guid> UpdateAllergyTypeAsync(AllergyType request) => Task.FromResult(request.Id);
        public Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId) => Task.FromResult(allergyTypeId);
        public Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> SearchAllergyTypesAsync(
            PageQuery query,
            int offset) => throw new NotSupportedException();
    }
}
