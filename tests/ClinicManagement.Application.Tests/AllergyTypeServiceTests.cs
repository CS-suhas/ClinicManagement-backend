using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.AllergyTypes.Services;
using ClinicManagement.Domain.Entities;

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

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateAllergyTypeAsync(new AllergyTypeRequest(Guid.NewGuid(), "Peanuts")));

        Assert.Null(repository.UpdatedAllergyType);
    }

    private sealed class FakeAllergyTypeRepository : IAllergyTypeRepository
    {
        public AllergyType? AddedAllergyType { get; private set; }
        public AllergyType? UpdatedAllergyType { get; private set; }

        public Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId) =>
            Task.FromResult<AllergyType?>(null);
        public Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync() =>
            Task.FromResult<IEnumerable<AllergyType>>(Array.Empty<AllergyType>());
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
        public Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId) => Task.FromResult(allergyTypeId);
    }
}
