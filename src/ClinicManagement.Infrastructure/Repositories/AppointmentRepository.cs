using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Domain.Entities;
using Dapper;
using Infrastructure.Data;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Infrastructure.Repositories;

public class AppointmentRepository(IDbConnectionFactory connectionFactory) : IAppointmentRepository
{
    public async Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId)
    {
        const string sql = """
            SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit,
                   p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            WHERE a.Id = :Id
            """;

        return (await QueryAppointmentsAsync(sql, new { Id = appointmentId.ToString() })).SingleOrDefault();
    }

    public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync()
    {
        const string sql = """
            SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit,
                   p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            """;

        return await QueryAppointmentsAsync(sql);
    }

    public async Task<Guid> AddAppointmentAsync(Appointment appointment, Guid patientId, Guid providerId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO Appointments (Id, PatientId, ProviderId, AppointmentDateTime, ReasonForVisit)
            VALUES (:Id, :PatientId, :ProviderId, :AppointmentDateTime, :ReasonForVisit)
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

    public async Task<Guid> DeleteAppointmentAsync(Guid appointmentId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM Appointments WHERE Id = :Id";
        await connection.ExecuteAsync(sql, new { Id = appointmentId.ToString() });
        return appointmentId;
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByProviderIdAsync(Guid providerId)
    {
        const string sql = """
            SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit,
                   p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            WHERE a.ProviderId = :ProviderId
            ORDER BY a.AppointmentDateTime, a.Id
            """;

        return await QueryAppointmentsAsync(sql, new { ProviderId = providerId.ToString() });
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(Guid patientId)
    {
        const string sql = """
            SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit,
                   p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            WHERE a.PatientId = :PatientId
            ORDER BY a.AppointmentDateTime, a.Id
            """;

        return await QueryAppointmentsAsync(sql, new { PatientId = patientId.ToString() });
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByClinicIdAsync(Guid clinicId)
    {
        const string sql = """
            SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit,
                   p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   pr.Id, pr.FirstName, pr.LastName, pr.Specialty, pr.ClinicId
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
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
                (UPPER(p.FirstName) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(p.LastName) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(p.FirstName || ' ' || p.LastName) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(pr.FirstName) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(pr.LastName) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(pr.FirstName || ' ' || pr.LastName) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(pr.Specialty) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(c.Name) LIKE '%' || UPPER(:Search) || '%'
                 OR UPPER(a.ReasonForVisit) LIKE '%' || UPPER(:Search) || '%')
                """);
            parameters.Add("Search", search);
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
        if (query.PatientId is { } patientId)
        {
            conditions.Add("a.PatientId = :PatientId");
            parameters.Add("PatientId", patientId.ToString());
        }

        var where = conditions.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", conditions)}";
        const string joins = """
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            INNER JOIN Clinics c ON c.Id = pr.ClinicId
            """;
        var totalCount = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) {joins} {where}",
            parameters);

        parameters.Add("Offset", offset);
        parameters.Add("PageSize", query.PageSize);
        var items = (await connection.QueryAsync<AppointmentListResponse>(
            $"""
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
            {joins}
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
        const string sql = """
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
            WHERE a.AppointmentDateTime >= :StartDateTime
            ORDER BY a.AppointmentDateTime, a.Id
            FETCH FIRST :Count ROWS ONLY
            """;
        var appointments = await connection.QueryAsync<AppointmentListResponse>(
            sql,
            new { StartDateTime = from, Count = count });
        return appointments.AsList();
    }

    public async Task<bool> HasProviderAppointmentOverlapAsync(
        Guid providerId,
        DateTimeOffset appointmentStart,
        DateTimeOffset appointmentEnd,
        Guid? excludedAppointmentId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT COUNT(1)
            FROM Appointments
            WHERE ProviderId = :ProviderId
              AND AppointmentDateTime > :AppointmentStart
              AND AppointmentDateTime < :AppointmentEnd
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