using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Clinics.Services;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Tests;

public class ClinicServiceTests
{
    [Fact]
    public async Task AddClinicAsync_PersistsClinicAndReturnsItsId()
    {
        var repository = new FakeClinicRepository();
        var service = CreateService(repository);
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
        var service = CreateService(repository);
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
        var service = CreateService(repository);

        await service.UpdateClinicAsync(new ClinicRequest(clinicId, "Downtown Clinic", null));

        Assert.NotNull(repository.UpdatedClinic);
        Assert.Null(repository.UpdatedClinic.PhoneNumber);
    }

    [Fact]
    public async Task DeleteClinicAsync_ThrowsWhenClinicDoesNotExist()
    {
        var repository = new FakeClinicRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.DeleteClinicAsync(Guid.NewGuid()));

        Assert.Null(repository.DeletedClinicId);
    }

    [Fact]
    public async Task GetProvidersByClinicIdAsync_ReturnsClinicProviders()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var provider = Provider.Create(Guid.NewGuid(), "Grace", "Hopper", "Computing", clinic);
        var providers = new FakeProviderRepository();
        providers.Providers.Add(provider);
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            providers);

        var result = await service.GetProvidersByClinicIdAsync(clinic.Id);

        var response = Assert.Single(result);
        Assert.Equal(provider.Id, response.Id);
        Assert.Equal(clinic.Id, response.ClinicId);
    }

    [Fact]
    public async Task GetAppointmentsByClinicIdAsync_ReturnsAppointmentsForClinicProviders()
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
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            new FakeProviderRepository(),
            appointments);

        var result = await service.GetAppointmentsByClinicIdAsync(clinic.Id);

        var response = Assert.Single(result);
        Assert.Equal(appointment.Id, response.Id);
        Assert.Equal(provider.Id, response.ProviderId);
    }

    [Fact]
    public async Task GetProvidersByClinicIdAsync_ThrowsWhenClinicDoesNotExist()
    {
        var service = CreateService(new FakeClinicRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetProvidersByClinicIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AddClinicAdminAsync_StoresAdminForExistingClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Downtown Clinic", "10 Main St", "555-0100");
        var adminRepository = new FakeClinicAdminRepository();
        var userRepository = new FakeUserRepository();
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            clinicAdminRepository: adminRepository,
            userRepository: userRepository);

        var id = await service.AddClinicAdminAsync(clinic.Id, " subject-123 ");

        Assert.NotNull(adminRepository.Added);
        Assert.Equal(id, adminRepository.Added.Id);
        Assert.Equal(clinic.Id, adminRepository.Added.ClinicId);
        var user = Assert.Single(userRepository.Users);
        Assert.Equal("subject-123", user.IdentitySubject);
        Assert.Equal(user.Id, adminRepository.Added.UserId);
    }

    [Fact]
    public async Task AddClinicAdminAsync_ThrowsNotFoundForUnknownClinic()
    {
        var adminRepository = new FakeClinicAdminRepository();
        var service = CreateService(new FakeClinicRepository(), clinicAdminRepository: adminRepository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.AddClinicAdminAsync(Guid.NewGuid(), "subject-123"));

        Assert.Null(adminRepository.Added);
    }

    [Fact]
    public async Task AddClinicAdminAsync_ThrowsConflictWhenIdentityAlreadyAdministersClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Downtown Clinic", "10 Main St", "555-0100");
        var adminRepository = new FakeClinicAdminRepository { AlreadyAdmin = true };
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            clinicAdminRepository: adminRepository);

        await Assert.ThrowsAsync<ClinicAdminAlreadyAssignedException>(
            () => service.AddClinicAdminAsync(clinic.Id, "subject-123"));

        Assert.Null(adminRepository.Added);
    }

    [Fact]
    public async Task GetAppointmentsByClinicIdAsync_ForbidsClinicAdminOfAnotherClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            currentUser: new TestCurrentUser { AdministeredClinicIds = [Guid.NewGuid()] });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAppointmentsByClinicIdAsync(clinic.Id));
    }

    [Fact]
    public async Task GetAppointmentsByClinicIdAsync_AllowsClinicAdminOfThatClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinic", null, null);
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            currentUser: new TestCurrentUser { AdministeredClinicIds = [clinic.Id] });

        var result = await service.GetAppointmentsByClinicIdAsync(clinic.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllClinicsAsync_CountsVerifiedProvidersPerClinic()
    {
        var riverside = Clinic.Create(Guid.NewGuid(), "Riverside", "1 River Road", "555-0100");
        var hillside = Clinic.Create(Guid.NewGuid(), "Hillside", "2 Hill Road", "555-0101");
        var empty = Clinic.Create(Guid.NewGuid(), "Empty", "3 Field Road", "555-0102");
        var providers = new FakeProviderRepository();
        providers.Providers.Add(Provider.Create(Guid.NewGuid(), riverside.Id, "Ada", "Lovelace", "Computing"));
        providers.Providers.Add(Provider.Create(Guid.NewGuid(), riverside.Id, "Alan", "Turing", "Computing"));
        providers.Providers.Add(Provider.Create(Guid.NewGuid(), hillside.Id, "Grace", "Hopper", "Navy"));
        var clinics = new FakeClinicRepository();
        clinics.AllClinics.AddRange([riverside, hillside, empty]);
        var service = CreateService(clinics, providers);

        var result = (await service.GetAllClinicsAsync()).ToDictionary(clinic => clinic.Name);

        Assert.Equal(2, result["Riverside"].ProviderCount);
        Assert.Equal(1, result["Hillside"].ProviderCount);
        Assert.Equal(0, result["Empty"].ProviderCount);
        Assert.Equal("1 River Road", result["Riverside"].Address);
    }

    [Fact]
    public async Task SearchClinicsAsync_IncludesProviderCountOnEachItem()
    {
        var riverside = Clinic.Create(Guid.NewGuid(), "Riverside", "1 River Road", "555-0100");
        var providers = new FakeProviderRepository();
        providers.Providers.Add(Provider.Create(Guid.NewGuid(), riverside.Id, "Ada", "Lovelace", "Computing"));
        var clinics = new FakeClinicRepository();
        clinics.AllClinics.Add(riverside);
        var service = CreateService(clinics, providers);

        var page = await service.SearchClinicsAsync(new PageQuery());

        Assert.Equal(1, page.TotalCount);
        Assert.Equal(riverside.Id, page.Items.Single().Id);
        Assert.Equal(1, page.Items.Single().ProviderCount);
    }

    [Fact]
    public async Task GetClinicSummaryAsync_ThrowsNotFoundForUnknownClinic()
    {
        var service = CreateService(new FakeClinicRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetClinicSummaryAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetClinicSummaryAsync_RequiresSignIn()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Riverside", "1 River Road", "555-0100");
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            currentUser: new TestCurrentUser { IsAuthenticated = false, Subject = null });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetClinicSummaryAsync(clinic.Id));
    }

    [Fact]
    public async Task GetClinicSummaryAsync_CountsVerifiedProvidersRanksSpecialtiesAndPreviewsFive()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Riverside", "1 River Road", "555-0100");
        var providers = new FakeProviderRepository();
        (string First, string Last, string Specialty)[] roster =
        [
            ("Zed", "Adams", "Cardiology"),
            ("Amy", "Baker", "Neurology"),
            ("Cal", "Baker", "Cardiology"),
            ("Eve", "Carter", "Dermatology"),
            ("Gus", "Davis", "Oncology"),
            ("Hal", "Evans", "Pediatrics"),
            ("Ivy", "Fox", "Pediatrics")
        ];
        foreach (var (first, last, specialty) in roster)
            providers.Providers.Add(Provider.Create(Guid.NewGuid(), clinic.Id, first, last, specialty));
        var service = CreateService(new FakeClinicRepository { ExistingClinic = clinic }, providers);

        var summary = await service.GetClinicSummaryAsync(clinic.Id);

        Assert.Equal(clinic.Id, summary.Clinic.Id);
        Assert.Equal(7, summary.ProviderCount);
        Assert.Equal(["Cardiology", "Pediatrics", "Dermatology"], summary.TopSpecialties);
        Assert.Equal(
            ["Zed Adams", "Amy Baker", "Cal Baker", "Eve Carter", "Gus Davis"],
            summary.Providers.Select(provider => $"{provider.FirstName} {provider.LastName}").ToArray());
    }

    [Fact]
    public async Task GetClinicSummaryAsync_UsesTheUtcDayForTodayAppointments()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Riverside", "1 River Road", "555-0100");
        var status = new FakeStatusRepository { TodayAppointmentCount = 4 };
        var service = CreateService(
            new FakeClinicRepository { ExistingClinic = clinic },
            statusRepository: status,
            now: new DateTimeOffset(2030, 1, 2, 23, 30, 0, TimeSpan.Zero));

        var summary = await service.GetClinicSummaryAsync(clinic.Id);

        Assert.Equal(4, summary.TodayAppointmentCount);
        Assert.Equal(new DateTimeOffset(2030, 1, 2, 0, 0, 0, TimeSpan.Zero), status.ClinicCountsRequest?.TodayStart);
        Assert.Equal(new DateTimeOffset(2030, 1, 3, 0, 0, 0, TimeSpan.Zero), status.ClinicCountsRequest?.TodayEnd);
        Assert.Equal([clinic.Id], status.ClinicCountsRequest?.ClinicIds.ToArray() ?? []);
    }

    [Fact]
    public async Task GetClinicSummaryAsync_ShowsPendingJoinRequestsOnlyToAdministratorsOfThisClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Riverside", "1 River Road", "555-0100");
        var otherClinic = Clinic.Create(Guid.NewGuid(), "Hillside", "2 Hill Road", "555-0101");
        var status = new FakeStatusRepository { PendingJoinRequestCount = 3 };

        async Task<int?> PendingFor(TestCurrentUser user)
        {
            var service = CreateService(
                new FakeClinicRepository { ExistingClinic = clinic },
                currentUser: user,
                statusRepository: status);
            return (await service.GetClinicSummaryAsync(clinic.Id)).PendingJoinRequestCount;
        }

        Assert.Equal(3, await PendingFor(new TestCurrentUser { Roles = ["admin"] }));
        Assert.Equal(3, await PendingFor(new TestCurrentUser { Roles = ["clinicadmin"], AdministeredClinicIds = [clinic.Id] }));
        Assert.Null(await PendingFor(new TestCurrentUser { Roles = ["clinicadmin"], AdministeredClinicIds = [otherClinic.Id] }));
        Assert.Null(await PendingFor(new TestCurrentUser { Roles = ["doctor"] }));
        Assert.Equal([clinic.Id], status.PendingJoinRequestClinicIds?.ToArray() ?? []);
    }

    [Fact]
    public async Task GetClinicSummaryAsync_ReturnsEmptyListsForClinicWithoutVerifiedProviders()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Riverside", "1 River Road", "555-0100");
        var service = CreateService(new FakeClinicRepository { ExistingClinic = clinic });

        var summary = await service.GetClinicSummaryAsync(clinic.Id);

        Assert.Equal(0, summary.ProviderCount);
        Assert.Empty(summary.TopSpecialties);
        Assert.Empty(summary.Providers);
    }

    private static ClinicService CreateService(
        FakeClinicRepository clinicRepository,
        FakeProviderRepository? providerRepository = null,
        FakeAppointmentRepository? appointmentRepository = null,
        FakeClinicAdminRepository? clinicAdminRepository = null,
        TestCurrentUser? currentUser = null,
        FakeStatusRepository? statusRepository = null,
        DateTimeOffset? now = null,
        FakeUserRepository? userRepository = null) =>
        new(
            clinicRepository,
            providerRepository ?? new FakeProviderRepository(),
            appointmentRepository ?? new FakeAppointmentRepository(),
            clinicAdminRepository ?? new FakeClinicAdminRepository(),
            userRepository ?? new FakeUserRepository(),
            statusRepository ?? new FakeStatusRepository(),
            currentUser ?? new TestCurrentUser { Roles = ["admin"] },
            new FixedTimeProvider(now ?? DefaultNow));

    private static readonly DateTimeOffset DefaultNow = new(2030, 1, 2, 9, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStatusRepository : IStatusRepository
    {
        public int TodayAppointmentCount { get; init; }
        public int PendingJoinRequestCount { get; init; }
        public (IReadOnlyCollection<Guid> ClinicIds, DateTimeOffset TodayStart, DateTimeOffset TodayEnd)? ClinicCountsRequest { get; private set; }
        public IReadOnlyCollection<Guid>? PendingJoinRequestClinicIds { get; private set; }

        public Task<DashboardScopeCounts> GetClinicCountsAsync(
            IReadOnlyCollection<Guid> clinicIds,
            DateTimeOffset todayStart,
            DateTimeOffset todayEnd)
        {
            ClinicCountsRequest = (clinicIds, todayStart, todayEnd);
            return Task.FromResult(new DashboardScopeCounts(1, 0, 0, 0, TodayAppointmentCount));
        }

        public Task<int> CountPendingJoinRequestsAsync(IReadOnlyCollection<Guid> clinicIds)
        {
            PendingJoinRequestClinicIds = clinicIds;
            return Task.FromResult(PendingJoinRequestCount);
        }

        public Task<StatusStatistics> GetStatisticsAsync() => throw new NotSupportedException();
        public Task<DashboardResponse> GetDashboardAsync(DateTimeOffset now) => throw new NotSupportedException();
        public Task<DashboardScopeCounts> GetPlatformCountsAsync(DateTimeOffset todayStart, DateTimeOffset todayEnd) =>
            throw new NotSupportedException();
        public Task<int> CountAllPendingJoinRequestsAsync() => throw new NotSupportedException();
        public Task<DoctorDashboardSection> GetDoctorDashboardAsync(
            Guid providerId,
            DateTimeOffset now,
            DateTimeOffset todayStart,
            DateTimeOffset todayEnd,
            DateTimeOffset weekEnd) => throw new NotSupportedException();
        public Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now) =>
            throw new NotSupportedException();
    }

    private sealed class FakeClinicAdminRepository : IClinicAdminRepository
    {
        public bool AlreadyAdmin { get; init; }
        public ClinicAdmin? Added { get; private set; }

        public Task<bool> IsAdminOfClinicAsync(Guid userId, Guid clinicId) =>
            Task.FromResult(AlreadyAdmin);

        public Task<IReadOnlyList<Guid>> GetClinicIdsAsync(Guid userId) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task AddAsync(ClinicAdmin clinicAdmin)
        {
            Added = clinicAdmin;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClinicRepository : IClinicRepository
    {
        public Clinic? AddedClinic { get; private set; }
        public Clinic? ExistingClinic { get; init; }
        public Clinic? UpdatedClinic { get; private set; }
        public Guid? DeletedClinicId { get; private set; }

        public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId ? ExistingClinic : null);
        public List<Clinic> AllClinics { get; } = [];
        public Task<IEnumerable<Clinic>> GetAllClinicsAsync() =>
            Task.FromResult<IEnumerable<Clinic>>(AllClinics.ToList());
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
        public Task<bool> ClinicExistsAsync(Guid clinicId) =>
            Task.FromResult(ExistingClinic?.Id == clinicId);
        public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset) =>
            Task.FromResult<(IReadOnlyList<Clinic> Items, int TotalCount)>(
                (AllClinics.Skip(offset).Take(query.PageSize).ToList(), AllClinics.Count));
    }

    private sealed class FakeProviderRepository : IProviderRepository
    {
        public Task<IReadOnlyDictionary<Guid, int>> GetVerifiedProviderCountsByClinicAsync() =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(Providers
                .Where(provider => provider.VerificationStatus == ClinicManagement.Domain.Enums.ProviderVerificationStatus.VERIFIED
                    && provider.ClinicId is not null)
                .GroupBy(provider => provider.ClinicId!.Value)
                .ToDictionary(group => group.Key, group => group.Count()));
        public Task<(IReadOnlyList<ClinicManagement.Application.Models.ProviderPatientRosterItem> Items, int TotalCount)>
            SearchPatientRosterAsync(
                Guid providerId,
                ClinicManagement.Application.Models.PageQuery query,
                int offset,
                DateTimeOffset now) => throw new NotSupportedException();
        public Task<Provider?> GetByUserIdAsync(Guid userId) => Task.FromResult<Provider?>(null);
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId) => throw new NotSupportedException();
        public Task<Guid> AddRegistrationAsync(Provider provider) => throw new NotSupportedException();
        public Task<bool> TryUpdateVerificationStatusAsync(
            Guid providerId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus expected,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus next) => throw new NotSupportedException();
        public List<Provider> Providers { get; } = [];

        public Task<Provider?> GetProviderByIdAsync(Guid providerId) => Task.FromResult<Provider?>(null);
        public Task<IEnumerable<Provider>> GetAllProvidersAsync() =>
            Task.FromResult<IEnumerable<Provider>>(Providers);
        public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
            Task.FromResult<IEnumerable<Provider>>(Providers.Where(provider => provider.ClinicId == clinicId));
        public Task<Guid> AddProviderAsync(Provider provider, Guid clinicId) => Task.FromResult(provider.Id);
        public Task<Guid> UpdateProviderAsync(Provider provider, Guid clinicId) => Task.FromResult(provider.Id);
        public Task<Guid> DeleteProviderAsync(Guid providerId) => Task.FromResult(providerId);
        public Task<bool> HasAppointmentsAsync(Guid providerId) => Task.FromResult(false);
        public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
            ClinicManagement.Application.Models.PageQuery query,
            int offset,
            Guid? clinicId,
            ClinicManagement.Domain.Enums.ProviderVerificationStatus? verificationStatus = null) =>
            throw new NotSupportedException();
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
