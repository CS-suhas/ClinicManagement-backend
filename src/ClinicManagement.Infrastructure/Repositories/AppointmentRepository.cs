using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Domain.Entities;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Repositories;

public class AppointmentRepository(IDbConnectionFactory connectionFactory) : IAppointmentRepository
{
    public async Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit,
                   p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   pr.Id, pr.FirstName, pr.LastName, pr.Specialty
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            WHERE a.Id = @Id
            """;

        var appointment = await connection.QueryAsync<Appointment, Patient, Provider, Appointment>(
            sql,
            (item, patient, provider) =>
            {
                item.SetPatient(patient);
                item.SetProvider(provider);
                return item;
            },
            new { Id = appointmentId },
            splitOn: "Id,Id");

        return appointment.SingleOrDefault();
    }

    public async Task<IEnumerable<Appointment>> GetAllAppointmentsAsync()
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            SELECT a.Id, a.AppointmentDateTime, a.ReasonForVisit,
                   p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.PhoneNumber, p.Email,
                   pr.Id, pr.FirstName, pr.LastName, pr.Specialty
            FROM Appointments a
            INNER JOIN Patients p ON p.Id = a.PatientId
            INNER JOIN Providers pr ON pr.Id = a.ProviderId
            """;

        return await connection.QueryAsync<Appointment, Patient, Provider, Appointment>(
            sql,
            (item, patient, provider) =>
            {
                item.SetPatient(patient);
                item.SetProvider(provider);
                return item;
            },
            splitOn: "Id,Id");
    }

    public async Task<Guid> AddAppointmentAsync(Appointment appointment)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            INSERT INTO Appointments (Id, PatientId, ProviderId, AppointmentDateTime, ReasonForVisit)
            VALUES (@Id, @PatientId, @ProviderId, @AppointmentDateTime, @ReasonForVisit)
            """;
        await connection.ExecuteAsync(sql, new
        {
            appointment.Id,
            PatientId = appointment.Patient.Id,
            ProviderId = appointment.Provider.Id,
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit
        });
        return appointment.Id;
    }

    public async Task<Guid> UpdateAppointmentAsync(Appointment appointment)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = """
            UPDATE Appointments
            SET PatientId = @PatientId,
                ProviderId = @ProviderId,
                AppointmentDateTime = @AppointmentDateTime,
                ReasonForVisit = @ReasonForVisit
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, new
        {
            appointment.Id,
            PatientId = appointment.Patient.Id,
            ProviderId = appointment.Provider.Id,
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit
        });
        return appointment.Id;
    }

    public async Task<Guid> DeleteAppointmentAsync(Guid appointmentId)
    {
        using var connection = connectionFactory.CreateConnection();
        const string sql = "DELETE FROM Appointments WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = appointmentId });
        return appointmentId;
    }
}