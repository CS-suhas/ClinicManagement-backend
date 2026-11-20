using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Clinics.Services;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Tests;

public class ClinicServiceTests
{
    [Fact]
    public async Task AddClinicAsync_PersistsClinicAndReturnsItsId()
    {
        var repository = new FakeClinicRepository();
        var service = new ClinicService(repository);
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
        var service = new ClinicService(repository);
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
        var service = new ClinicService(repository);

        await service.UpdateClinicAsync(new ClinicRequest(clinicId, "Downtown Clinic", null));

        Assert.NotNull(repository.UpdatedClinic);
        Assert.Null(repository.UpdatedClinic.PhoneNumber);
    }

    [Fact]
    public async Task DeleteClinicAsync_ThrowsWhenClinicDoesNotExist()
    {
        var repository = new FakeClinicRepository();
        var service = new ClinicService(repository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.DeleteClinicAsync(Guid.NewGuid()));

        Assert.Null(repository.DeletedClinicId);
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
        public Task<bool> ClinicExistsAsync(Guid clinicId) => Task.FromResult(false);
    }
}
