using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Services;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Tests;

public class MeDashboardServiceTests
{
    // 23:30 at UTC+2 on 2030-03-10 is 21:30 UTC on the same day.
    private static readonly DateTimeOffset LateEveningAtPlusTwo = new(2030, 3, 10, 23, 30, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset UtcTodayStart = new(2030, 3, 10, 0, 0, 0, TimeSpan.Zero);

    private static readonly AppointmentListResponse NextVisit = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Ada Lovelace",
        Guid.NewGuid(),
        "Alan Turing",
        "Computing",
        Guid.NewGuid(),
        "Riverside Clinic",
        new DateTimeOffset(2030, 3, 11, 9, 0, 0, TimeSpan.Zero),
        "Checkup",
        "CONFIRMED",
        0,
        null,
        new DateTimeOffset(2030, 3, 1, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2030, 3, 1, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task GetDashboardAsync_RequiresSignedInUser()
    {
        var service = CreateService(new FakeStatusRepository(), new TestCurrentUser { IsAuthenticated = false, Subject = null });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDashboardAsync());
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsNullSectionsAndQueriesNothingWithoutRoles()
    {
        var repository = new FakeStatusRepository();
        var service = CreateService(repository, new TestCurrentUser { Roles = [] });

        var result = await service.GetDashboardAsync();

        Assert.Null(result.Admin);
        Assert.Null(result.ClinicAdmin);
        Assert.Null(result.Doctor);
        Assert.Null(result.Patient);
        Assert.Null(repository.PlatformWindow);
        Assert.Null(repository.PatientRequest);
    }

    [Fact]
    public async Task GetDashboardAsync_FillsAdminSectionFromPlatformCounts()
    {
        var repository = new FakeStatusRepository
        {
            PlatformCounts = new DashboardScopeCounts(3, 10, 40, 2, 5)
        };
        var service = CreateService(repository, new TestCurrentUser { Roles = ["admin"] });

        var result = await service.GetDashboardAsync();

        Assert.Equal(new AdminDashboardSection(3, 10, 40, 2, 5), result.Admin);
        Assert.Null(result.ClinicAdmin);
        Assert.Equal((UtcTodayStart, UtcTodayStart.AddDays(1)), repository.PlatformWindow);
    }

    [Fact]
    public async Task GetDashboardAsync_ScopesClinicAdminSectionToAdministeredClinics()
    {
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var repository = new FakeStatusRepository
        {
            ClinicCounts = new DashboardScopeCounts(2, 4, 9, 1, 3)
        };
        var service = CreateService(repository, new TestCurrentUser
        {
            Roles = ["clinicadmin"],
            AdministeredClinicIds = [clinicA, clinicB]
        });

        var result = await service.GetDashboardAsync();

        Assert.Equal(new ClinicAdminDashboardSection(2, 4, 9, 1, 3), result.ClinicAdmin);
        Assert.Equal<Guid>(new[] { clinicA, clinicB }, repository.ClinicIdsQueried);
        Assert.Null(result.Admin);
    }

    [Fact]
    public async Task GetDashboardAsync_ClinicAdminWithoutClinicsGetsZeroesWithoutQuerying()
    {
        var repository = new FakeStatusRepository();
        var service = CreateService(repository, new TestCurrentUser { Roles = ["clinicadmin"] });

        var result = await service.GetDashboardAsync();

        Assert.Equal(new ClinicAdminDashboardSection(0, 0, 0, 0, 0), result.ClinicAdmin);
        Assert.Null(repository.ClinicIdsQueried);
    }

    [Fact]
    public async Task GetDashboardAsync_FillsDoctorSectionWithUtcWindows()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Alan", "Turing", "Computing");
        var repository = new FakeStatusRepository
        {
            DoctorSection = new DoctorDashboardSection(1, 4, 7, NextVisit, 3)
        };
        var service = CreateService(repository, new TestCurrentUser { Roles = ["doctor"], Provider = provider });

        var result = await service.GetDashboardAsync();

        Assert.Equal(new DoctorDashboardSection(1, 4, 7, NextVisit, 3), result.Doctor);
        Assert.Equal(provider.Id, repository.DoctorRequest?.ProviderId);
        Assert.Equal(LateEveningAtPlusTwo, repository.DoctorRequest?.Now);
        Assert.Equal(UtcTodayStart, repository.DoctorRequest?.TodayStart);
        Assert.Equal(UtcTodayStart.AddDays(1), repository.DoctorRequest?.TodayEnd);
        Assert.Equal(UtcTodayStart.AddDays(7), repository.DoctorRequest?.WeekEnd);
    }

    [Fact]
    public async Task GetDashboardAsync_DoctorRoleWithoutLinkedProviderGetsNoSection()
    {
        var repository = new FakeStatusRepository();
        var service = CreateService(repository, new TestCurrentUser { Roles = ["doctor"] });

        var result = await service.GetDashboardAsync();

        Assert.Null(result.Doctor);
        Assert.Null(repository.DoctorRequest);
    }

    [Fact]
    public async Task GetDashboardAsync_FillsPatientSectionForLinkedPatient()
    {
        var patient = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        var repository = new FakeStatusRepository
        {
            PatientSection = new PatientDashboardSection(NextVisit, 2, [])
        };
        var service = CreateService(repository, new TestCurrentUser { Roles = ["patient"], Patient = patient });

        var result = await service.GetDashboardAsync();

        Assert.Equal(new PatientDashboardSection(NextVisit, 2, []), result.Patient);
        Assert.Equal(patient.Id, repository.PatientRequest?.PatientId);
        Assert.Equal(LateEveningAtPlusTwo, repository.PatientRequest?.Now);
    }

    [Fact]
    public async Task GetDashboardAsync_ReturnsEverySectionTheCallerHolds()
    {
        var provider = Provider.Create(Guid.NewGuid(), Guid.NewGuid(), "Alan", "Turing", "Computing");
        var patient = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0100");
        var service = CreateService(new FakeStatusRepository(), new TestCurrentUser
        {
            Roles = ["doctor", "patient"],
            Provider = provider,
            Patient = patient
        });

        var result = await service.GetDashboardAsync();

        Assert.NotNull(result.Doctor);
        Assert.NotNull(result.Patient);
        Assert.Null(result.Admin);
        Assert.Null(result.ClinicAdmin);
    }

    [Fact]
    public async Task GetDashboardAsync_UsesTheUtcCalendarDayNotTheCallersLocalDay()
    {
        var repository = new FakeStatusRepository();
        // 01:00 at UTC+2 is 23:00 UTC on the previous day.
        var atLocalOneAm = new DateTimeOffset(2030, 3, 10, 1, 0, 0, TimeSpan.FromHours(2));
        var service = CreateService(repository, new TestCurrentUser { Roles = ["admin"] }, atLocalOneAm);

        await service.GetDashboardAsync();

        Assert.Equal(new DateTimeOffset(2030, 3, 9, 0, 0, 0, TimeSpan.Zero), repository.PlatformWindow?.Start);
    }

    private static MeDashboardService CreateService(
        FakeStatusRepository repository,
        TestCurrentUser currentUser,
        DateTimeOffset? now = null) =>
        new(repository, currentUser, new FixedClock(now ?? LateEveningAtPlusTwo));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStatusRepository : IStatusRepository
    {
        public DashboardScopeCounts PlatformCounts { get; init; } = new(0, 0, 0, 0, 0);
        public DashboardScopeCounts ClinicCounts { get; init; } = new(0, 0, 0, 0, 0);
        public DoctorDashboardSection DoctorSection { get; init; } = new(0, 0, 0, null, 0);
        public PatientDashboardSection PatientSection { get; init; } = new(null, 0, []);

        public (DateTimeOffset Start, DateTimeOffset End)? PlatformWindow { get; private set; }
        public List<Guid>? ClinicIdsQueried { get; private set; }
        public (Guid ProviderId, DateTimeOffset Now, DateTimeOffset TodayStart, DateTimeOffset TodayEnd, DateTimeOffset WeekEnd)? DoctorRequest { get; private set; }
        public (Guid PatientId, DateTimeOffset Now)? PatientRequest { get; private set; }

        public Task<StatusStatistics> GetStatisticsAsync() => throw new NotSupportedException();
        public Task<DashboardResponse> GetDashboardAsync(DateTimeOffset now) => throw new NotSupportedException();

        public Task<DashboardScopeCounts> GetPlatformCountsAsync(DateTimeOffset todayStart, DateTimeOffset todayEnd)
        {
            PlatformWindow = (todayStart, todayEnd);
            return Task.FromResult(PlatformCounts);
        }

        public Task<DashboardScopeCounts> GetClinicCountsAsync(
            IReadOnlyCollection<Guid> clinicIds,
            DateTimeOffset todayStart,
            DateTimeOffset todayEnd)
        {
            ClinicIdsQueried = clinicIds.ToList();
            return Task.FromResult(ClinicCounts);
        }

        public Task<DoctorDashboardSection> GetDoctorDashboardAsync(
            Guid providerId,
            DateTimeOffset now,
            DateTimeOffset todayStart,
            DateTimeOffset todayEnd,
            DateTimeOffset weekEnd)
        {
            DoctorRequest = (providerId, now, todayStart, todayEnd, weekEnd);
            return Task.FromResult(DoctorSection);
        }

        public Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now)
        {
            PatientRequest = (patientId, now);
            return Task.FromResult(PatientSection);
        }
    }
}
