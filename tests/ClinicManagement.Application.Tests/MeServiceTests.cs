using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Services;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class MeServiceTests
{
    [Fact]
    public async Task GetCurrentUserAsync_ReturnsNullLinksWhenNothingIsLinked()
    {
        var service = new MeService(new FakePatientRepository(), new TestCurrentUser { Subject = "kc-1", Roles = ["doctor"] });

        var result = await service.GetCurrentUserAsync();

        Assert.Equal("kc-1", result.Subject);
        Assert.Equal(["doctor"], result.Roles);
        Assert.Null(result.Patient);
        Assert.Null(result.Provider);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsLinkedPatientAndProviderVerificationStatus()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        var provider = Provider.Register(Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var service = new MeService(
            new FakePatientRepository(),
            new TestCurrentUser { Subject = "kc-2", Patient = patient, Provider = provider });

        var result = await service.GetCurrentUserAsync();

        Assert.Equal(patient.Id, result.Patient?.Id);
        Assert.Equal(provider.Id, result.Provider?.Id);
        Assert.Equal("PENDING", result.Provider?.VerificationStatus);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsDisplayNameAndAdministeredClinics()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var service = new MeService(
            new FakePatientRepository(),
            new TestCurrentUser
            {
                Subject = "kc-3",
                Roles = ["clinicadmin"],
                DisplayName = "Dr. Ada",
                AdministeredClinicIds = [clinicA, clinicB]
            });

        var result = await service.GetCurrentUserAsync();

        Assert.Equal("Dr. Ada", result.DisplayName);
        Assert.Equal([clinicA, clinicB], result.ClinicAdminOf);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ReturnsNullDisplayNameAndNoClinicsWhenNoneAreKnown()
    {
        var service = new MeService(new FakePatientRepository(), new TestCurrentUser { Subject = "kc-4", Roles = ["patient"] });

        var result = await service.GetCurrentUserAsync();

        Assert.Null(result.DisplayName);
        Assert.Empty(result.ClinicAdminOf);
    }

    [Fact]
    public async Task CreatePatientProfileAsync_LinksNewPatientToSubject()
    {
        var repository = new FakePatientRepository();
        var service = new MeService(repository, new TestCurrentUser { Subject = "kc-9", Roles = ["patient"] });

        var id = await service.CreatePatientProfileAsync(
            new PatientRequest(Guid.Empty, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com"));

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(id, repository.AddedPatient?.Id);
        Assert.Equal("kc-9", repository.AddedPatient?.IdentitySubject);
    }

    [Fact]
    public async Task CreatePatientProfileAsync_RejectsSecondProfileForSameIdentity()
    {
        var existing = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        var repository = new FakePatientRepository();
        var service = new MeService(repository, new TestCurrentUser { Subject = "kc-9", Roles = ["patient"], Patient = existing });

        await Assert.ThrowsAsync<IdentityAlreadyLinkedException>(
            () => service.CreatePatientProfileAsync(
                new PatientRequest(Guid.Empty, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com")));

        Assert.Null(repository.AddedPatient);
    }

    [Fact]
    public async Task CreatePatientProfileAsync_AllowsSignedInUserWithoutPatientRole()
    {
        var repository = new FakePatientRepository();
        var service = new MeService(repository, new TestCurrentUser { Subject = "kc-new", Roles = [] });

        var id = await service.CreatePatientProfileAsync(
            new PatientRequest(Guid.Empty, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com"));

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("kc-new", repository.AddedPatient?.IdentitySubject);
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        public Patient? AddedPatient { get; private set; }

        public Task<Guid> AddPatientAsync(Patient patient)
        {
            AddedPatient = patient;
            return Task.FromResult(patient.Id);
        }

        public Task<Patient?> GetByIdentitySubjectAsync(string identitySubject) => Task.FromResult<Patient?>(null);
        public Task<Patient?> GetPatientByIdAsync(Guid patientId) => throw new NotSupportedException();
        public Task<IEnumerable<Patient>> GetAllPatientsAsync() => throw new NotSupportedException();
        public Task<Guid> UpdatePatientAsync(Patient patient) => throw new NotSupportedException();
        public Task<Guid> DeletePatientAsync(Guid patientId) => throw new NotSupportedException();
        public Task<bool> PatientExistsAsync(Guid patientId) => throw new NotSupportedException();
        public Task<bool> HasDependentRecordsAsync(Guid patientId) => throw new NotSupportedException();
        public Task<IEnumerable<PatientAllergy>> GetAllergiesByPatientIdAsync(Guid patientId) => throw new NotSupportedException();
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
            int offset) => throw new NotSupportedException();
        public Task<Guid> UpsertPatientAllergyAsync(
            Guid patientId,
            Guid allergyId,
            AllergySeverity severity,
            string? notes) => throw new NotSupportedException();
        public Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId) => throw new NotSupportedException();
    }
}
