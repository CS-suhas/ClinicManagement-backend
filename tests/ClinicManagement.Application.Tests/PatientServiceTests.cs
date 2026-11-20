using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Patients.Services;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class PatientServiceTests
{
    [Fact]
    public async Task GetPatientByIdAsync_ThrowsWhenPatientDoesNotExist()
    {
        var service = new PatientService(new FakePatientRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetPatientByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddPatientAsync_PersistsTrimmedPatientAndReturnsItsId()
    {
        var repository = new FakePatientRepository();
        var service = new PatientService(repository);
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
        var repository = new FakePatientRepository();
        var service = new PatientService(repository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.UpdatePatientAsync(CreatePatientRequest(Guid.NewGuid())));

        Assert.Null(repository.UpdatedPatient);
    }

    [Fact]
    public async Task UpdatePatientAsync_RejectsInvalidEmailBeforeLookingUpPatient()
    {
        var repository = new FakePatientRepository();
        var service = new PatientService(repository);

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
        var service = new PatientService(repository);

        await Assert.ThrowsAsync<PatientHasRecordsException>(
            () => service.DeletePatientAsync(patient.Id));

        Assert.Null(repository.DeletedPatientId);
    }

    private static PatientRequest CreatePatientRequest(Guid id) =>
        new(id, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com");

    private sealed class FakePatientRepository : IPatientRepository
    {
        public Patient? AddedPatient { get; private set; }
        public Patient? UpdatedPatient { get; private set; }
        public Patient? ExistingPatient { get; init; }
        public bool HasDependentRecords { get; init; }
        public Guid? DeletedPatientId { get; private set; }
        public int PatientLookupCount { get; private set; }

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
    }
}
