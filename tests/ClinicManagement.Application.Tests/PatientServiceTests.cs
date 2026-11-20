using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Patients.Services;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Tests;

public class PatientServiceTests
{
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

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdatePatientAsync(CreatePatientRequest(Guid.NewGuid())));

        Assert.Null(repository.UpdatedPatient);
    }

    private static PatientRequest CreatePatientRequest(Guid id) =>
        new(id, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com");

    private sealed class FakePatientRepository : IPatientRepository
    {
        public Patient? AddedPatient { get; private set; }
        public Patient? UpdatedPatient { get; private set; }

        public Task<Patient?> GetPatientByIdAsync(Guid patientId) => Task.FromResult<Patient?>(null);
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
        public Task<Guid> DeletePatientAsync(Guid patientId) => Task.FromResult(patientId);
        public Task<bool> PatientExistsAsync(Guid patientId) => Task.FromResult(false);
    }
}
