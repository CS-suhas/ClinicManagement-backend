using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Services;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class AllergyTypeServiceTests
{
    [Fact]
    public async Task AddAllergyTypeAsync_PersistsTrimmedNameAndReturnsItsId()
    {
        var repository = new FakeAllergyTypeRepository();
        var service = new AllergyTypeService(repository);
        var request = new AllergyTypeRequest(Guid.NewGuid(), "  Peanuts  ");

        var result = await service.AddAllergyTypeAsync(request);

        Assert.Equal(request.Id, result);
        Assert.NotNull(repository.AddedAllergyType);
        Assert.Equal("Peanuts", repository.AddedAllergyType.Name);
    }

    [Fact]
    public async Task UpdateAllergyTypeAsync_ThrowsWhenAllergyTypeDoesNotExist()
    {
        var repository = new FakeAllergyTypeRepository();
        var service = new AllergyTypeService(repository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.UpdateAllergyTypeAsync(new AllergyTypeRequest(Guid.NewGuid(), "Peanuts")));

        Assert.Null(repository.UpdatedAllergyType);
    }

    [Fact]
    public async Task GetPatientsByAllergyTypeIdAsync_ReturnsPatientsRecordedWithTheAllergy()
    {
        var repository = new FakeAllergyTypeRepository();
        var allergyType = AllergyType.Create(Guid.NewGuid(), "Peanuts");
        repository.ExistingAllergyType = allergyType;
        repository.PatientsWithAllergyType.Add(
            Patient.Create(Guid.NewGuid(), "Ava", "Patient", new DateOnly(1990, 1, 1), "ava@example.com", "555-0100"));
        var service = new AllergyTypeService(repository);

        var result = await service.GetPatientsByAllergyTypeIdAsync(allergyType.Id);

        var patient = Assert.Single(result);
        Assert.Equal("Ava", patient.FirstName);
        Assert.Equal("Patient", patient.LastName);
    }

    [Fact]
    public async Task GetPatientsByAllergyTypeIdAsync_ThrowsWhenAllergyTypeDoesNotExist()
    {
        var service = new AllergyTypeService(new FakeAllergyTypeRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetPatientsByAllergyTypeIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAllergyTypeAsync_ThrowsWhenAllergyIsRecordedForPatients()
    {
        var allergyType = AllergyType.Create(Guid.NewGuid(), "Peanuts");
        var repository = new FakeAllergyTypeRepository
        {
            ExistingAllergyType = allergyType,
            HasPatientsWithAllergyType = true
        };
        var service = new AllergyTypeService(repository);

        await Assert.ThrowsAsync<AllergyTypeInUseException>(
            () => service.DeleteAllergyTypeAsync(allergyType.Id));

        Assert.Null(repository.DeletedAllergyTypeId);
    }

    private sealed class FakeAllergyTypeRepository : IAllergyTypeRepository
    {
        public Task<IReadOnlyDictionary<Guid, int>> GetPatientCountsByAllergyTypeAsync() =>
            throw new NotSupportedException();

        public AllergyType? AddedAllergyType { get; private set; }
        public AllergyType? UpdatedAllergyType { get; private set; }
        public AllergyType? ExistingAllergyType { get; set; }
        public List<Patient> PatientsWithAllergyType { get; } = [];
        public bool HasPatientsWithAllergyType { get; set; }
        public Guid? DeletedAllergyTypeId { get; private set; }

        public Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId) =>
            Task.FromResult(ExistingAllergyType?.Id == allergyTypeId ? ExistingAllergyType : null);
        public Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync() =>
            Task.FromResult<IEnumerable<AllergyType>>(Array.Empty<AllergyType>());
        public Task<IEnumerable<Patient>> GetPatientsByAllergyTypeIdAsync(Guid allergyTypeId) =>
            Task.FromResult<IEnumerable<Patient>>(PatientsWithAllergyType);
        public Task<bool> HasPatientsWithAllergyTypeAsync(Guid allergyTypeId) =>
            Task.FromResult(HasPatientsWithAllergyType);
        public Task<Guid> AddAllergyTypeAsync(AllergyType allergyType)
        {
            AddedAllergyType = allergyType;
            return Task.FromResult(allergyType.Id);
        }
        public Task<Guid> UpdateAllergyTypeAsync(AllergyType allergyType)
        {
            UpdatedAllergyType = allergyType;
            return Task.FromResult(allergyType.Id);
        }
        public Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId)
        {
            DeletedAllergyTypeId = allergyTypeId;
            return Task.FromResult(allergyTypeId);
        }
        public Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> SearchAllergyTypesAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset) => throw new NotSupportedException();
    }
}
