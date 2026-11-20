using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Domain.Entities;
using Dapper;
using Infrastructure.Data;

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