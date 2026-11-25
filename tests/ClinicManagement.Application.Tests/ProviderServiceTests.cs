using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Providers.Services;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;
using PageQuery = ClinicManagement.Application.Models.PageQuery;
using ProviderPatientRosterItem = ClinicManagement.Application.Models.ProviderPatientRosterItem;

namespace ClinicManagement.Application.Tests;

public class ProviderServiceTests
{
    [Fact]
    public async Task RegisterProviderAsync_StoresAPendingProviderAtTheChosenClinic()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(providerRepository, new FakeClinicRepository { Exists = true }, new FakeAppointmentRepository(), new TestCurrentUser());
        var id = Guid.NewGuid();
        var clinicId = Guid.NewGuid();

        var result = await service.RegisterProviderAsync(new ProviderRegistrationRequest(id, clinicId, " Grace ", "Hopper", "Computing"));

        Assert.Equal(id, result);
        var registered = Assert.IsType<Provider>(providerRepository.Registered);
        Assert.Equal(ProviderVerificationStatus.PENDING, registered.VerificationStatus);
        Assert.Equal(clinicId, registered.ClinicId);
        Assert.Equal(clinicId, providerRepository.AddedClinicId);
        Assert.Equal("Grace", registered.FirstName);
    }

    [Fact]
    public async Task RegisterProviderAsync_RejectsBlankSpecialtyBeforeStoring()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository(), new TestCurrentUser());

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.RegisterProviderAsync(new ProviderRegistrationRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", " ")));

        Assert.Equal("specialty", exception.PropertyName);
        Assert.Null(providerRepository.Registered);
    }

    [Fact]
    public async Task RegisterProviderAsync_ThrowsWhenClinicDoesNotExist()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(providerRepository, new FakeClinicRepository { Exists = false }, new FakeAppointmentRepository(), new TestCurrentUser());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.RegisterProviderAsync(new ProviderRegistrationRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing")));

        Assert.Null(providerRepository.Registered);
    }

    [Fact]
    public async Task VerifyProviderAsync_MovesPendingProviderToVerified()
    {
        var pending = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        pending.AssignUser(Guid.NewGuid());
        var providerRepository = new FakeProviderRepository { ExistingProvider = pending };
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository(), new TestCurrentUser());

        await service.VerifyProviderAsync(pending.Id);

        Assert.Equal(
            (ProviderVerificationStatus.PENDING, ProviderVerificationStatus.VERIFIED),
            providerRepository.VerificationChange);
    }

    [Fact]
    public async Task RejectProviderAsync_ThrowsWhenProviderIsAlreadyVerified()
    {
        var verified = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var providerRepository = new FakeProviderRepository { ExistingProvider = verified };
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository(), new TestCurrentUser());

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(() => service.RejectProviderAsync(verified.Id));

        Assert.Null(providerRepository.VerificationChange);
    }

    [Fact]
    public async Task VerifyProviderAsync_ThrowsConflictWhenStatusChangedConcurrently()
    {
        var pending = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        pending.AssignUser(Guid.NewGuid());
        var providerRepository = new FakeProviderRepository
        {
            ExistingProvider = pending,
            VerificationChangeSucceeds = false
        };
        var service = new ProviderService(providerRepository, new FakeClinicRepository(), new FakeAppointmentRepository(), new TestCurrentUser());

        await Assert.ThrowsAsync<ConcurrentModificationException>(() => service.VerifyProviderAsync(pending.Id));
    }

    [Fact]
    public async Task GetProviderCalendarAsync_RejectsInvertedAndOversizedRanges()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Provider = provider });
        var from = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var inverted = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetProviderCalendarAsync(provider.Id, from, from.AddDays(-1), false));
        var oversized = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetProviderCalendarAsync(provider.Id, from, from.AddDays(63), false));

        Assert.Equal("from", inverted.PropertyName);
        Assert.Equal("to", oversized.PropertyName);
    }

    [Fact]
    public async Task GetProviderCalendarAsync_ForbidsProviderOtherThanTheCaller()
    {
        var caller = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = caller },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Provider = caller });
        var from = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetProviderCalendarAsync(Guid.NewGuid(), from, from.AddDays(7), false));
    }

    [Fact]
    public async Task GetProviderByIdAsync_ReturnsFirstAndLastNameSeparately()
    {
        var provider = Provider.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(), new TestCurrentUser());

        var result = await service.GetProviderByIdAsync(provider.Id);

        Assert.Equal("Grace", result.FirstName);
        Assert.Equal("Hopper", result.LastName);
    }

    [Fact]
    public async Task GetProviderByIdAsync_ReturnsTheClinicNameOfTheProvidersClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Riverside Clinic", "1 River Road", "555-0100");
        var provider = Provider.Create(Guid.NewGuid(), "Alan", "Turing", "Computing", clinic);
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(), new TestCurrentUser());

        var result = await service.GetProviderByIdAsync(provider.Id);

        Assert.Equal("Riverside Clinic", result.ClinicName);
    }

    [Fact]
    public async Task GetProviderByIdAsync_LeavesClinicNameNullWithoutAClinic()
    {
        var provider = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Edsger", "Dijkstra", "Algorithms");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(), new TestCurrentUser());

        var result = await service.GetProviderByIdAsync(provider.Id);

        Assert.Null(result.ClinicName);
    }

    [Fact]
    public async Task AddProviderAsync_PersistsProviderForExistingClinic()
    {
        var clinicId = Guid.NewGuid();
        var providerRepository = new FakeProviderRepository();
        var clinicRepository = new FakeClinicRepository { Exists = true };
        var service = new ProviderService(providerRepository, clinicRepository, new FakeAppointmentRepository(), new TestCurrentUser());
        var request = new ProviderRequest(Guid.NewGuid(), clinicId, "  Grace ", " Hopper  ", "  Computing ");

        var result = await service.AddProviderAsync(request);

        Assert.Equal(request.Id, result);
        Assert.NotNull(providerRepository.AddedProvider);
        Assert.Equal(clinicId, providerRepository.AddedClinicId);
        Assert.Equal(clinicId, providerRepository.AddedProvider.ClinicId);
        Assert.Equal("Grace", providerRepository.AddedProvider.FirstName);
        Assert.Equal("Hopper", providerRepository.AddedProvider.LastName);
        Assert.Equal("Computing", providerRepository.AddedProvider.Specialty);
    }

    [Fact]
    public async Task AddProviderAsync_StoresFeeAndGender()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository { Exists = true },
            new FakeAppointmentRepository(), new TestCurrentUser());

        await service.AddProviderAsync(
            new ProviderRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing", 1500.75m, " female "));

        Assert.NotNull(providerRepository.AddedProvider);
        Assert.Equal(1500.75m, providerRepository.AddedProvider.Fee);
        Assert.Equal(Gender.FEMALE, providerRepository.AddedProvider.Gender);
    }

    [Fact]
    public async Task AddProviderAsync_LeavesFeeAndGenderUnsetWhenOmitted()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository { Exists = true },
            new FakeAppointmentRepository(), new TestCurrentUser());

        await service.AddProviderAsync(
            new ProviderRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing"));

        Assert.NotNull(providerRepository.AddedProvider);
        Assert.Null(providerRepository.AddedProvider.Fee);
        Assert.Null(providerRepository.AddedProvider.Gender);
    }

    [Fact]
    public async Task AddProviderAsync_RejectsUnknownGenderBeforeStoring()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository { Exists = true },
            new FakeAppointmentRepository(), new TestCurrentUser());

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.AddProviderAsync(
                new ProviderRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing", Gender: "unknown")));

        Assert.Equal("gender", exception.PropertyName);
        Assert.Null(providerRepository.AddedProvider);
    }

    [Fact]
    public async Task RegisterProviderAsync_StoresFeeAndGender()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(providerRepository, new FakeClinicRepository { Exists = true }, new FakeAppointmentRepository(), new TestCurrentUser());

        await service.RegisterProviderAsync(
            new ProviderRegistrationRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing", 900m, "OTHER"));

        var registered = Assert.IsType<Provider>(providerRepository.Registered);
        Assert.Equal(900m, registered.Fee);
        Assert.Equal(Gender.OTHER, registered.Gender);
    }

    [Fact]
    public async Task GetProviderByIdAsync_ReturnsFeeAndGender()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        provider.SetFee(2000m);
        provider.SetGender(Gender.MALE);
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(), new TestCurrentUser());

        var result = await service.GetProviderByIdAsync(provider.Id);

        Assert.Equal(2000m, result.Fee);
        Assert.Equal("MALE", result.Gender);
    }

    [Fact]
    public async Task AddProviderAsync_ThrowsWhenClinicDoesNotExist()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository { Exists = false },
            new FakeAppointmentRepository(), new TestCurrentUser());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.AddProviderAsync(
                new ProviderRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing")));

        Assert.Null(providerRepository.AddedProvider);
    }

    [Fact]
    public async Task DeleteProviderAsync_ThrowsWhenProviderHasAppointments()
    {
        var provider = Provider.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Grace",
            "Hopper",
            "Computing");
        var providerRepository = new FakeProviderRepository
        {
            ExistingProvider = provider,
            HasAppointments = true
        };
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository(), new TestCurrentUser());

        await Assert.ThrowsAsync<ProviderHasAppointmentsException>(
            () => service.DeleteProviderAsync(provider.Id));

        Assert.Null(providerRepository.DeletedProviderId);
    }

    [Fact]
    public async Task GetClinicByProviderIdAsync_ReturnsTheProvidersClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", "Address", "555-0100");
        var provider = Provider.Create(Guid.NewGuid(), "Grace", "Hopper", "Computing", clinic);
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository { ExistingClinic = clinic },
            new FakeAppointmentRepository(), new TestCurrentUser());

        var result = await service.GetClinicByProviderIdAsync(provider.Id);

        Assert.Equal(clinic.Id, result.Id);
    }

    [Fact]
    public async Task GetAppointmentsByProviderIdAsync_ReturnsProviderAppointments()
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
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository { ExistingClinic = clinic },
            appointments, new TestCurrentUser { Roles = ["admin"] });

        var result = await service.GetAppointmentsByProviderIdAsync(provider.Id);

        var response = Assert.Single(result);
        Assert.Equal(appointment.Id, response.Id);
        Assert.Equal(provider.Id, response.ProviderId);
    }

    private static readonly Guid RegisteringUserId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    [Fact]
    public async Task RegisterProviderAsync_LinksProviderToSignedInUser()
    {
        var providerRepository = new FakeProviderRepository();
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository { Exists = true },
            new FakeAppointmentRepository(),
            new TestCurrentUser { Subject = "kc-user-1", UserId = RegisteringUserId });

        await service.RegisterProviderAsync(new ProviderRegistrationRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing"));

        Assert.Equal(RegisteringUserId, providerRepository.Registered?.UserId);
    }

    [Fact]
    public async Task RegisterProviderAsync_RejectsSubjectThatAlreadyHasAProvider()
    {
        var existing = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        existing.AssignUser(RegisteringUserId);
        var providerRepository = new FakeProviderRepository { ExistingByUserId = existing };
        var service = new ProviderService(
            providerRepository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Subject = "kc-user-1", UserId = RegisteringUserId });

        await Assert.ThrowsAsync<IdentityAlreadyLinkedException>(
            () => service.RegisterProviderAsync(new ProviderRegistrationRequest(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing")));

        Assert.Null(providerRepository.Registered);
    }

    [Fact]
    public async Task GetProviderCalendarAsync_ForbidsUnverifiedProvider()
    {
        var pending = Provider.Register(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = pending },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Provider = pending });
        var from = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetProviderCalendarAsync(pending.Id, from, from.AddDays(7), false));
    }

    [Fact]
    public async Task GetPatientRosterAsync_ReturnsPagedRosterForTheProviderItself()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var repository = new FakeProviderRepository { ExistingProvider = provider };
        var now = new DateTimeOffset(2030, 1, 1, 9, 0, 0, TimeSpan.Zero);
        var service = new ProviderService(
            repository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Provider = provider, Roles = ["doctor"] },
            new FixedClock(now));

        var page = await service.GetPatientRosterAsync(provider.Id, new PageQuery(Search: " Ada ", Page: 2, PageSize: 10));

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(provider.Id, repository.RosterRequest?.ProviderId);
        Assert.Equal(10, repository.RosterRequest?.Offset);
        Assert.Equal("Ada", repository.RosterRequest?.Query.NormalizedSearch);
        Assert.Equal(now, repository.RosterRequest?.Now);
    }

    [Fact]
    public async Task GetPatientRosterAsync_AllowsAdministrator()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var repository = new FakeProviderRepository { ExistingProvider = provider };
        var service = new ProviderService(
            repository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Roles = ["admin"] });

        await service.GetPatientRosterAsync(provider.Id, new PageQuery());

        Assert.NotNull(repository.RosterRequest);
    }

    [Fact]
    public async Task GetPatientRosterAsync_AllowsClinicAdminOfTheProvidersClinic()
    {
        var clinicId = Guid.NewGuid();
        var provider = Provider.Create(Guid.NewGuid(), clinicId, "Grace", "Hopper", "Computing");
        var repository = new FakeProviderRepository { ExistingProvider = provider };
        var service = new ProviderService(
            repository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { AdministeredClinicIds = [clinicId] });

        await service.GetPatientRosterAsync(provider.Id, new PageQuery());

        Assert.NotNull(repository.RosterRequest);
    }

    [Fact]
    public async Task GetPatientRosterAsync_ForbidsAnotherDoctorWithoutQuerying()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var other = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Alan", "Turing", "Computing");
        var repository = new FakeProviderRepository { ExistingProvider = provider };
        var service = new ProviderService(
            repository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Provider = other, Roles = ["doctor"] });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetPatientRosterAsync(provider.Id, new PageQuery()));
        Assert.Null(repository.RosterRequest);
    }

    [Fact]
    public async Task GetPatientRosterAsync_ForbidsClinicAdminOfAnotherClinic()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { AdministeredClinicIds = [Guid.NewGuid()] });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetPatientRosterAsync(provider.Id, new PageQuery()));
    }

    [Fact]
    public async Task GetPatientRosterAsync_ThrowsNotFoundForUnknownProvider()
    {
        var service = new ProviderService(
            new FakeProviderRepository(),
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Roles = ["admin"] });

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetPatientRosterAsync(Guid.NewGuid(), new PageQuery()));
    }

    [Fact]
    public async Task GetPatientRosterAsync_RejectsInvalidPageBeforeQuerying()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var repository = new FakeProviderRepository { ExistingProvider = provider };
        var service = new ProviderService(
            repository,
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { Roles = ["admin"] });

        await Assert.ThrowsAsync<DomainValidationException>(
            () => service.GetPatientRosterAsync(provider.Id, new PageQuery(Page: 0)));
        Assert.Null(repository.RosterRequest);
    }

    [Fact]
    public async Task GetAppointmentsByProviderIdAsync_ForbidsClinicAdminOfAnotherClinic()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Grace", "Hopper", "Computing");
        var service = new ProviderService(
            new FakeProviderRepository { ExistingProvider = provider },
            new FakeClinicRepository(),
            new FakeAppointmentRepository(),
            new TestCurrentUser { AdministeredClinicIds = [Guid.NewGuid()] });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAppointmentsByProviderIdAsync(provider.Id));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeProviderRepository : IProviderRepository
    {
        public Task<IReadOnlyDictionary<Guid, int>> GetVerifiedProviderCountsByClinicAsync() =>
            throw new NotSupportedException();
        public Provider? ExistingByUserId { get; init; }
        public (Guid ProviderId, PageQuery Query, int Offset, DateTimeOffset Now)? RosterRequest { get; private set; }

        public Task<(IReadOnlyList<ProviderPatientRosterItem> Items, int TotalCount)> SearchPatientRosterAsync(
            Guid providerId,
            PageQuery query,
            int offset,
            DateTimeOffset now)
        {
            RosterRequest = (providerId, query, offset, now);
            return Task.FromResult<(IReadOnlyList<ProviderPatientRosterItem>, int)>(([], 2));
        }
        public Task<Provider?> GetByUserIdAsync(Guid userId) =>
            Task.FromResult(ExistingByUserId?.UserId == userId ? ExistingByUserId : null);
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId) => throw new NotSupportedException();
        public Provider? Registered => AddedProvider;
        public bool VerificationChangeSucceeds { get; init; } = true;
        public (ProviderVerificationStatus Expected, ProviderVerificationStatus Next)? VerificationChange
        {
            get; private set;
        }

        public Task<bool> TryUpdateVerificationStatusAsync(
            Guid providerId,
            ProviderVerificationStatus expected,
            ProviderVerificationStatus next)
        {
            if (VerificationChangeSucceeds) VerificationChange = (expected, next);
            return Task.FromResult(VerificationChangeSucceeds);
        }
        public Provider? AddedProvider { get; private set; }
        public Guid? AddedClinicId { get; private set; }
        public Provider? ExistingProvider { get; init; }
        public bool HasAppointments { get; init; }
        public Guid? DeletedProviderId { get; private set; }

        public Task<Provider?> GetProviderByIdAsync(Guid providerId) =>
            Task.FromResult(ExistingProvider?.Id == providerId ? ExistingProvider : null);
        public Task<IEnumerable<Provider>> GetAllProvidersAsync() =>
            Task.FromResult<IEnumerable<Provider>>(Array.Empty<Provider>());
        public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
            Task.FromResult<IEnumerable<Provider>>(Array.Empty<Provider>());
        public Task<Guid> AddProviderAsync(Provider provider, Guid clinicId)
        {
            AddedProvider = provider;
            AddedClinicId = clinicId;
            return Task.FromResult(provider.Id);
        }
        public Task<Guid> UpdateProviderAsync(Provider provider, Guid clinicId) => Task.FromResult(provider.Id);
        public Task<Guid> DeleteProviderAsync(Guid providerId)
        {
            DeletedProviderId = providerId;
            return Task.FromResult(providerId);
        }
        public Task<bool> HasAppointmentsAsync(Guid providerId) => Task.FromResult(HasAppointments);
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus? verificationStatus = null) =>
            throw new NotSupportedException();
    }

    private sealed class FakeClinicRepository : IClinicRepository
    {
        public bool Exists { get; init; }
        public Clinic? ExistingClinic { get; init; }

        public Task<bool> ClinicExistsAsync(Guid clinicId) => Task.FromResult(Exists);
        public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId ? ExistingClinic : null);
        public Task<IEnumerable<Clinic>> GetAllClinicsAsync() =>
            Task.FromResult<IEnumerable<Clinic>>(Array.Empty<Clinic>());
        public Task<Guid> AddClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> UpdateClinicAsync(Clinic clinic) => Task.FromResult(clinic.Id);
        public Task<Guid> DeleteClinicAsync(Guid clinicId) => Task.FromResult(clinicId);
        public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset) => throw new NotSupportedException();
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public Task<IReadOnlyList<DateTimeOffset>> GetActiveStartTimesInRangeAsync(
            Guid providerId,
            DateTimeOffset start,
            DateTimeOffset end) => throw new NotSupportedException();

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
