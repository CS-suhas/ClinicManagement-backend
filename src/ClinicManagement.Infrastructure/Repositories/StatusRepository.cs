using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using Dapper;
using Infrastructure.Data;
using ClinicManagement.Application.Appointments.Models;

namespace ClinicManagement.Infrastructure.Repositories;

public sealed class StatusRepository(IDbConnectionFactory connectionFactory) : IStatusRepository
{
    public async Task<StatusStatistics> GetStatisticsAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM Clinics) AS Clinics,
                (SELECT COUNT(1) FROM Providers) AS Providers,
                (SELECT COUNT(1) FROM Patients) AS Patients,
                (SELECT COUNT(1) FROM Appointments) AS Appointments
            FROM DUAL
            """;
        var counts = await connection.QuerySingleAsync<StatisticsCounts>(sql);
        return new StatusStatistics(
            checked((int)counts.Clinics),
            checked((int)counts.Providers),
            checked((int)counts.Patients),
            checked((int)counts.Appointments));
    }

    public async Task<DashboardResponse> GetDashboardAsync(DateTimeOffset now)
    {
        using var connection = connectionFactory.CreateConnection();
        const string countsSql = """
            SELECT
                (SELECT COUNT(1) FROM Clinics) AS Clinics,
                (SELECT COUNT(1) FROM Providers) AS Providers,
                (SELECT COUNT(1) FROM Patients) AS Patients,
                (SELECT COUNT(1) FROM AllergyTypes) AS AllergyTypes,
                (SELECT COUNT(1) FROM Appointments) AS Appointments
            FROM DUAL
            """;
        const string appointmentsSql = """
            SELECT a.Id,
                   a.PatientId,
                   p.FirstName || ' ' || p.LastName AS PatientName,
                   a.ProviderId,
                   pr.FirstName || ' ' || pr.LastName AS ProviderName,
                   pr.Specialty AS ProviderSpecialty,
                   pr.ClinicId,
                   c.Name AS ClinicName,
                   a.AppointmentDateTime,
                   a.ReasonForVisit,
                   a.Status
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            INNER JOIN Clinics c ON c.Id = pr.ClinicId
            WHERE a.AppointmentDateTime >= :Now
              AND a.Status IN ('REQUESTED', 'CONFIRMED')
            ORDER BY a.AppointmentDateTime, a.Id
            FETCH FIRST 5 ROWS ONLY
            """;
        var counts = await connection.QuerySingleAsync<DashboardCounts>(countsSql);
        var upcomingAppointments = (await connection.QueryAsync<AppointmentListResponse>(
            appointmentsSql,
            new { Now = now })).AsList();
        return new DashboardResponse(
            checked((int)counts.Clinics),
            checked((int)counts.Providers),
            checked((int)counts.Patients),
            checked((int)counts.AllergyTypes),
            checked((int)counts.Appointments),
            upcomingAppointments);
    }

    private const string SelectListItem = """
        SELECT a.Id,
               a.PatientId,
               p.FirstName || ' ' || p.LastName AS PatientName,
               a.ProviderId,
               pr.FirstName || ' ' || pr.LastName AS ProviderName,
               pr.Specialty AS ProviderSpecialty,
               pr.ClinicId,
               c.Name AS ClinicName,
               a.AppointmentDateTime,
               a.ReasonForVisit,
               a.Status
        FROM Appointments a
        INNER JOIN Patients p ON p.Id = a.PatientId
        INNER JOIN Providers pr ON pr.Id = a.ProviderId
        INNER JOIN Clinics c ON c.Id = pr.ClinicId
        """;

    public async Task<DashboardScopeCounts> GetPlatformCountsAsync(DateTimeOffset todayStart, DateTimeOffset todayEnd)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT
                (SELECT COUNT(1) FROM Clinics) AS ClinicCount,
                (SELECT COUNT(1) FROM Providers) AS ProviderCount,
                (SELECT COUNT(1) FROM Patients) AS PatientCount,
                (SELECT COUNT(1) FROM Providers WHERE VerificationStatus = 'PENDING') AS PendingProviderVerificationCount,
                (SELECT COUNT(1) FROM Appointments
                 WHERE AppointmentDateTime >= :TodayStart AND AppointmentDateTime < :TodayEnd
                   AND Status NOT IN ('DECLINED', 'CANCELLED')) AS TodayAppointmentCount
            FROM DUAL
            """;
        var counts = await connection.QuerySingleAsync<ScopeCountsRow>(
            sql,
            new { TodayStart = todayStart, TodayEnd = todayEnd });
        return new DashboardScopeCounts(
            checked((int)counts.ClinicCount),
            checked((int)counts.ProviderCount),
            checked((int)counts.PatientCount),
            checked((int)counts.PendingProviderVerificationCount),
            checked((int)counts.TodayAppointmentCount));
    }

    public async Task<DashboardScopeCounts> GetClinicCountsAsync(
        IReadOnlyCollection<Guid> clinicIds,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            WITH scoped_providers AS (
                SELECT Id, VerificationStatus FROM Providers WHERE ClinicId IN :ClinicIds
            ),
            scoped_appointments AS (
                SELECT PatientId, AppointmentDateTime, Status FROM Appointments
                WHERE ProviderId IN (SELECT Id FROM scoped_providers)
            )
            SELECT
                (SELECT COUNT(1) FROM scoped_providers) AS ProviderCount,
                (SELECT COUNT(DISTINCT PatientId) FROM scoped_appointments) AS PatientCount,
                (SELECT COUNT(1) FROM scoped_providers WHERE VerificationStatus = 'PENDING') AS PendingProviderVerificationCount,
                (SELECT COUNT(1) FROM scoped_appointments
                 WHERE AppointmentDateTime >= :TodayStart AND AppointmentDateTime < :TodayEnd
                   AND Status NOT IN ('DECLINED', 'CANCELLED')) AS TodayAppointmentCount
            FROM DUAL
            """;
        var counts = await connection.QuerySingleAsync<ScopedCountsRow>(sql, new
        {
            ClinicIds = clinicIds.Select(id => id.ToString()).ToArray(),
            TodayStart = todayStart,
            TodayEnd = todayEnd
        });
        return new DashboardScopeCounts(
            clinicIds.Distinct().Count(),
            checked((int)counts.ProviderCount),
            checked((int)counts.PatientCount),
            checked((int)counts.PendingProviderVerificationCount),
            checked((int)counts.TodayAppointmentCount));
    }

    public async Task<int> CountPendingJoinRequestsAsync(IReadOnlyCollection<Guid> clinicIds)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT COUNT(1) FROM ClinicJoinRequests
            WHERE Status = 'PENDING' AND ClinicId IN :ClinicIds
            """;
        return await connection.ExecuteScalarAsync<int>(
            sql,
            new { ClinicIds = clinicIds.Select(id => id.ToString()).ToArray() });
    }

    public async Task<DoctorDashboardSection> GetDoctorDashboardAsync(
        Guid providerId,
        DateTimeOffset now,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd,
        DateTimeOffset weekEnd)
    {
        using var connection = connectionFactory.CreateConnection();
        const string countsSql = """
            SELECT
                (SELECT COUNT(1) FROM Appointments
                 WHERE ProviderId = :ProviderId
                   AND AppointmentDateTime >= :TodayStart AND AppointmentDateTime < :TodayEnd
                   AND Status NOT IN ('DECLINED', 'CANCELLED')) AS TodayAppointmentCount,
                (SELECT COUNT(1) FROM Appointments
                 WHERE ProviderId = :ProviderId
                   AND AppointmentDateTime >= :TodayStart AND AppointmentDateTime < :WeekEnd
                   AND Status NOT IN ('DECLINED', 'CANCELLED')) AS WeekAppointmentCount,
                (SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE ProviderId = :ProviderId) AS DistinctPatientCount
            FROM DUAL
            """;
        var counts = await connection.QuerySingleAsync<DoctorCountsRow>(countsSql, new
        {
            ProviderId = providerId.ToString(),
            TodayStart = todayStart,
            TodayEnd = todayEnd,
            WeekEnd = weekEnd
        });
        var nextAppointment = await connection.QueryFirstOrDefaultAsync<AppointmentListResponse>(
            $"""
            {SelectListItem}
            WHERE a.ProviderId = :ProviderId
              AND a.AppointmentDateTime >= :Now
              AND a.Status IN ('REQUESTED', 'CONFIRMED')
            ORDER BY a.AppointmentDateTime, a.Id
            FETCH FIRST 1 ROWS ONLY
            """,
            new { ProviderId = providerId.ToString(), Now = now });
        return new DoctorDashboardSection(
            checked((int)counts.TodayAppointmentCount),
            checked((int)counts.WeekAppointmentCount),
            checked((int)counts.DistinctPatientCount),
            nextAppointment);
    }

    public async Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now)
    {
        using var connection = connectionFactory.CreateConnection();
        var parameters = new { PatientId = patientId.ToString(), Now = now };
        var upcomingCount = await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1) FROM Appointments
            WHERE PatientId = :PatientId
              AND AppointmentDateTime >= :Now
              AND Status IN ('REQUESTED', 'CONFIRMED')
            """,
            parameters);
        var nextAppointment = await connection.QueryFirstOrDefaultAsync<AppointmentListResponse>(
            $"""
            {SelectListItem}
            WHERE a.PatientId = :PatientId
              AND a.AppointmentDateTime >= :Now
              AND a.Status IN ('REQUESTED', 'CONFIRMED')
            ORDER BY a.AppointmentDateTime, a.Id
            FETCH FIRST 1 ROWS ONLY
            """,
            parameters);
        return new PatientDashboardSection(nextAppointment, upcomingCount);
    }

    private sealed record ScopeCountsRow(
        decimal ClinicCount,
        decimal ProviderCount,
        decimal PatientCount,
        decimal PendingProviderVerificationCount,
        decimal TodayAppointmentCount);

    private sealed record ScopedCountsRow(
        decimal ProviderCount,
        decimal PatientCount,
        decimal PendingProviderVerificationCount,
        decimal TodayAppointmentCount);

    private sealed record DoctorCountsRow(
        decimal TodayAppointmentCount,
        decimal WeekAppointmentCount,
        decimal DistinctPatientCount);

    private sealed record StatisticsCounts(
        decimal Clinics,
        decimal Providers,
        decimal Patients,
        decimal Appointments);

    private sealed record DashboardCounts(
        decimal Clinics,
        decimal Providers,
        decimal Patients,
        decimal AllergyTypes,
        decimal Appointments);
}
