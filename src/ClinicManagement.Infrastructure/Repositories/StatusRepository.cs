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
                   a.ReasonForVisit
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            INNER JOIN Clinics c ON c.Id = pr.ClinicId
            WHERE a.AppointmentDateTime >= :Now
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
