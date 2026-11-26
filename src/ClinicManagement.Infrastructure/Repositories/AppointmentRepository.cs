using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using Dapper;
using ClinicManagement.Infrastructure.Data;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Infrastructure.Repositories;

public class AppointmentRepository(IDbConnectionFactory connectionFactory) : IAppointmentRepository
{
    private const string SelectAppointmentWithParties = """
        SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit, a.Status,
               p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
               pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId, pr.VerificationStatus
        FROM Appointments a
        INNER JOIN Patients p ON p.Id = a.PatientId
        INNER JOIN Providers pr ON pr.Id = a.ProviderId
        """;

    private const string SelectAppointmentListItem = AppointmentListSql.Columns;

    private const string ListItemJoins = AppointmentListSql.Joins;

    // Requested and confirmed appointments occupy the provider's time slot.
    private const string ActiveStatuses = "('REQUESTED', 'CONFIRMED')";

    public async Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId)
    {
        const string sql = $"{SelectAppointmentWithParties} WHERE a.Id = :Id";

        return (await QueryAppointmentsAsync(sql, new { Id = appointmentId.ToString() })).SingleOrDefault();
    }

    public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync() =>
        await QueryAppointmentsAsync(SelectAppointmentWithParties);

    public async Task<Guid> AddAppointmentAsync(Appointment appointment, Guid patientId, Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO Appointments (Id, PatientId, ProviderId, AppointmentDateTime, ReasonForVisit, Status)
            VALUES (:Id, :PatientId, :ProviderId, :AppointmentDateTime, :ReasonForVisit, :Status)
            """;
        await connection.ExecuteAsync(sql, new
        {
            Id = appointment.Id.ToString(),
            PatientId = patientId.ToString(),
            ProviderId = providerId.ToString(),
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit,
            Status = appointment.Status.ToString()
        });
        return appointment.Id;
    }

    public async Task<Guid> UpdateAppointmentAsync(Appointment appointment, Guid patientId, Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            UPDATE Appointments
            SET PatientId = :PatientId,
                ProviderId = :ProviderId,
                AppointmentDateTime = :AppointmentDateTime,
                ReasonForVisit = :ReasonForVisit,
                UpdatedAtUtc = SYSTIMESTAMP
            WHERE Id = :Id
            """;
        await connection.ExecuteAsync(sql, new
        {
            Id = appointment.Id.ToString(),
            PatientId = patientId.ToString(),
            ProviderId = providerId.ToString(),
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit
        });
        return appointment.Id;
    }

    public async Task<bool> TryUpdateStatusAsync(
        Guid appointmentId,
        AppointmentStatus expected,
        AppointmentStatus next)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            UPDATE Appointments
            SET Status = :Next,
                StatusChangedAtUtc = SYSTIMESTAMP,
                UpdatedAtUtc = SYSTIMESTAMP
            WHERE Id = :Id AND Status = :Expected
            """;
        var rows = await connection.ExecuteAsync(sql, new
        {
            Id = appointmentId.ToString(),
            Expected = expected.ToString(),
            Next = next.ToString()
        });
        return rows == 1;
    }

    public async Task<Guid> DeleteAppointmentAsync(Guid appointmentId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM Appointments WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = appointmentId.ToString() });
        return appointmentId;
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByProviderIdAsync(Guid providerId)
    {
        const string sql = $"""
            {SelectAppointmentWithParties}
            WHERE a.ProviderId = :ProviderId
            ORDER BY a.AppointmentDateTime, a.Id
            """;

        return await QueryAppointmentsAsync(sql, new { ProviderId = providerId.ToString() });
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(Guid patientId)
    {
        const string sql = $"""
            {SelectAppointmentWithParties}
            WHERE a.PatientId = :PatientId
            ORDER BY a.AppointmentDateTime, a.Id
            """;

        return await QueryAppointmentsAsync(sql, new { PatientId = patientId.ToString() });
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByClinicIdAsync(Guid clinicId)
    {
        const string sql = $"""
            {SelectAppointmentWithParties}
            WHERE pr.ClinicId = :ClinicId
            ORDER BY a.AppointmentDateTime, a.Id
            """;

        return await QueryAppointmentsAsync(sql, new { ClinicId = clinicId.ToString() });
    }

    public async Task<(IReadOnlyList<AppointmentListResponse> Items, int TotalCount)> SearchAppointmentsAsync(
        AppointmentSearchQuery query,
        int offset)
    {
        using var connection = connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        var conditions = new List<string>();
        if (query.NormalizedSearch is { } search)
        {
            conditions.Add("""
                (UPPER(p.FirstName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(p.LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(p.FirstName || ' ' || p.LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(pr.FirstName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(pr.LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(pr.FirstName || ' ' || pr.LastName) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(pr.Specialty) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(c.Name) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\'
                 OR UPPER(a.ReasonForVisit) LIKE '%' || UPPER(:Search) || '%' ESCAPE '\')
                """);
            parameters.Add("Search", SqlLike.Escape(search));
        }
        if (query.From is { } from)
        {
            conditions.Add("a.AppointmentDateTime >= :StartDateTime");
            parameters.Add("StartDateTime", from);
        }
        if (query.To is { } to)
        {
            conditions.Add("a.AppointmentDateTime <= :EndDateTime");
            parameters.Add("EndDateTime", to);
        }
        if (query.ClinicId is { } clinicId)
        {
            conditions.Add("pr.ClinicId = :ClinicId");
            parameters.Add("ClinicId", clinicId.ToString());
        }
        if (query.ProviderId is { } providerId)
        {
            conditions.Add("a.ProviderId = :ProviderId");
            parameters.Add("ProviderId", providerId.ToString());
        }
        if (query.ProviderIds is { Length: > 0 } providerIds)
        {
            conditions.Add("a.ProviderId IN :ProviderIds");
            parameters.Add("ProviderIds", providerIds.Select(id => id.ToString()).ToArray());
        }
        if (query.PatientId is { } patientId)
        {
            conditions.Add("a.PatientId = :PatientId");
            parameters.Add("PatientId", patientId.ToString());
        }
        if (query.Status is { } status)
        {
            conditions.Add("a.Status = :Status");
            parameters.Add("Status", status.ToString());
        }

        var where = conditions.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", conditions)}";
        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) {ListItemJoins} {where}",
            parameters);

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", query.PageSize);
        var items = (await connection.QueryAsync<AppointmentListResponse>(
            $"""
            {SelectAppointmentListItem}
            {ListItemJoins}
            {where}
            ORDER BY a.AppointmentDateTime, a.Id
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """,
            parameters)).AsList();
        return (items, totalCount);
    }

    public async Task<IReadOnlyList<AppointmentListResponse>> GetUpcomingAppointmentsAsync(
        DateTimeOffset from,
        int count)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = $"""
            {SelectAppointmentListItem}
            {ListItemJoins}
            WHERE a.AppointmentDateTime >= :StartDateTime
              AND a.Status IN {ActiveStatuses}
            ORDER BY a.AppointmentDateTime, a.Id
            FETCH FIRST :Count ROWS ONLY
            """;
        var appointments = await connection.QueryAsync<AppointmentListResponse>(
            sql,
            new { StartDateTime = from, Count = count });
        return appointments.AsList();
    }

    public async Task<IReadOnlyList<AppointmentListResponse>> GetProviderAppointmentsInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeInactive)
    {
        using var connection = connectionFactory.CreateConnection();
        var statusFilter = includeInactive ? string.Empty : "AND a.Status NOT IN ('DECLINED', 'CANCELLED')";
        var appointments = await connection.QueryAsync<AppointmentListResponse>(
            $"""
            {SelectAppointmentListItem}
            {ListItemJoins}
            WHERE a.ProviderId = :ProviderId
              AND a.AppointmentDateTime >= :StartDateTime
              AND a.AppointmentDateTime < :EndDateTime
              {statusFilter}
            ORDER BY a.AppointmentDateTime, a.Id
            """,
            new { ProviderId = providerId.ToString(), StartDateTime = start, EndDateTime = end });
        return appointments.AsList();
    }

    public async Task<IReadOnlyList<DateTimeOffset>> GetActiveStartTimesInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = $"""
            SELECT AppointmentDateTime
            FROM Appointments
            WHERE ProviderId = :ProviderId
              AND AppointmentDateTime >= :StartDateTime
              AND AppointmentDateTime < :EndDateTime
              AND Status IN {ActiveStatuses}
            ORDER BY AppointmentDateTime
            """;
        var rows = await connection.QueryAsync<ActiveStartTimeRow>(
            sql,
            new { ProviderId = providerId.ToString(), StartDateTime = start, EndDateTime = end });
        return rows.Select(row => row.AppointmentDateTime).ToList();
    }

    public async Task<bool> HasProviderAppointmentOverlapAsync(
        Guid providerId,
        DateTimeOffset appointmentStart,
        DateTimeOffset appointmentEnd,
        Guid? excludedAppointmentId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = $"""
            SELECT COUNT(1)
            FROM Appointments
            WHERE ProviderId = :ProviderId
              AND AppointmentDateTime > :AppointmentStart
              AND AppointmentDateTime < :AppointmentEnd
              AND Status IN {ActiveStatuses}
              AND (:ExcludedAppointmentId IS NULL OR Id <> :ExcludedAppointmentId)
            """;
        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            ProviderId = providerId.ToString(),
            AppointmentStart = appointmentStart,
            AppointmentEnd = appointmentEnd,
            ExcludedAppointmentId = excludedAppointmentId?.ToString()
        });
        return count > 0;
    }

    private sealed record ActiveStartTimeRow(DateTimeOffset AppointmentDateTime);

    private async Task<IEnumerable<Appointment>> QueryAppointmentsAsync(string sql, object? parameters = null)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QueryAsync<Appointment, Patient, Provider, Appointment>(
            sql,
            (appointment, patient, provider) =>
            {
                appointment.SetPatient(patient);
                appointment.SetProvider(provider);
                return appointment;
            },
            parameters,
            splitOn: "Id,Id");
    }
}
