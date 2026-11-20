using ClinicManagement.Domain.Entities;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class AppointmentRepositoryIntegrationTests
{
    [Fact]
    public async Task AppointmentRepository_PersistsAndUpdatesAppointmentInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinicRepository = new ClinicRepository(connectionFactory);
        var patientRepository = new PatientRepository(connectionFactory);
        var providerRepository = new ProviderRepository(connectionFactory);
        var repository = new AppointmentRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101");
        var patient = Patient.Create(
            patientId,
            "Ada",
            "Lovelace",
            new DateOnly(1985, 12, 10),
            $"ada.{patientId:N}@example.com",
            "555-0100");
        var provider = Provider.Create(providerId, clinicId, "Grace", "Hopper", "General Practice");
        var now = DateTimeOffset.UtcNow;
        var appointmentDateTime = now.AddDays(1);
        var appointment = Appointment.Create(
            appointmentId,
            patientId,
            providerId,
            appointmentDateTime,
            "Consultation",
            now);

        try
        {
            Assert.Equal(clinicId, await clinicRepository.AddClinicAsync(clinic));
            Assert.Equal(patientId, await patientRepository.AddPatientAsync(patient));
            Assert.Equal(providerId, await providerRepository.AddProviderAsync(provider, clinicId));
            Assert.Equal(
                appointmentId,
                await repository.AddAppointmentAsync(appointment, patientId, providerId));

            var storedAppointment = await repository.GetAppointmentByIdAsync(appointmentId);
            Assert.NotNull(storedAppointment);
            Assert.Equal(appointmentDateTime, storedAppointment.AppointmentDateTime);
            Assert.Equal("Consultation", storedAppointment.ReasonForVisit);
            Assert.Equal(patientId, storedAppointment.Patient.Id);
            Assert.Equal(providerId, storedAppointment.Provider.Id);
            Assert.Contains(
                await repository.GetAllAppointmentsAsync(),
                result => result.Id == appointmentId);
            Assert.Contains(
                await repository.GetAppointmentsByProviderIdAsync(providerId),
                result => result.Id == appointmentId);

            var updatedAppointmentDateTime = now.AddDays(2);
            appointment.SetAppointmentDateTime(updatedAppointmentDateTime, DateTimeOffset.UtcNow);
            appointment.SetReasonForVisit("Follow-up", DateTimeOffset.UtcNow);
            Assert.Equal(
                appointmentId,
                await repository.UpdateAppointmentAsync(appointment, patientId, providerId));

            var updatedAppointment = await repository.GetAppointmentByIdAsync(appointmentId);
            Assert.NotNull(updatedAppointment);
            Assert.Equal(updatedAppointmentDateTime, updatedAppointment.AppointmentDateTime);
            Assert.Equal("Follow-up", updatedAppointment.ReasonForVisit);

            Assert.Equal(appointmentId, await repository.DeleteAppointmentAsync(appointmentId));
            Assert.Null(await repository.GetAppointmentByIdAsync(appointmentId));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM Appointments WHERE Id = :Id", new { Id = appointmentId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Providers WHERE Id = :Id", new { Id = providerId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = clinicId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Patients WHERE Id = :Id", new { Id = patientId.ToString() });
        }
    }
}
