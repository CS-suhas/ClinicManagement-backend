using System.Data;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class StatusRepositoryIntegrationTests
{
    // A far-future day: no dev-dataset or other test rows fall in it, so window and dashboard results are predictable.
    private static readonly DateTimeOffset FarDay = new(2090, 3, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task StatusRepository_StatisticsIncreaseWithSeededRowsInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var appointments = new AppointmentRepository(connectionFactory);
        var repository = new StatusRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var before = await repository.GetStatisticsAsync();
        try
        {
            await clinics.AddClinicAsync(Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101"));
            await providers.AddProviderAsync(Provider.Create(providerId, clinicId, "Grace", "Hopper", "General Practice"), clinicId);
            await patients.AddPatientAsync(Patient.Create(patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), $"ada.{patientId:N}@example.com", "555-0100"));
            await appointments.AddAppointmentAsync(
                Appointment.Create(appointmentId, patientId, providerId, now.AddDays(1), "Consultation", now),
                patientId,
                providerId);

            // Other test classes insert rows concurrently, so the totals can only be checked as lower bounds.
            var after = await repository.GetStatisticsAsync();
            Assert.True(after.Clinics >= before.Clinics + 1);
            Assert.True(after.Providers >= before.Providers + 1);
            Assert.True(after.Patients >= before.Patients + 1);
            Assert.True(after.Appointments >= before.Appointments + 1);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM Appointments WHERE Id = :Id", new { Id = appointmentId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Providers WHERE Id = :Id", new { Id = providerId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Patients WHERE Id = :Id", new { Id = patientId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = clinicId.ToString() });
        }
    }

    [Fact]
    public async Task StatusRepository_DashboardReturnsCountsAndUpcomingActiveAppointmentsInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var appointments = new AppointmentRepository(connectionFactory);
        var repository = new StatusRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestedId = Guid.NewGuid();
        var confirmedId = Guid.NewGuid();
        var declinedId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var upcomingDay = new DateTimeOffset(2095, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var before = await repository.GetDashboardAsync(upcomingDay.AddDays(-1));
        try
        {
            await clinics.AddClinicAsync(Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101"));
            await providers.AddProviderAsync(Provider.Create(providerId, clinicId, "Grace", "Hopper", "General Practice"), clinicId);
            await patients.AddPatientAsync(Patient.Create(patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), $"ada.{patientId:N}@example.com", "555-0100"));

            await appointments.AddAppointmentAsync(
                Appointment.Create(requestedId, patientId, providerId, upcomingDay.AddHours(9), null, now), patientId, providerId);
            await appointments.AddAppointmentAsync(
                Appointment.Create(confirmedId, patientId, providerId, upcomingDay.AddHours(10), null, now), patientId, providerId);
            await appointments.AddAppointmentAsync(
                Appointment.Create(declinedId, patientId, providerId, upcomingDay.AddHours(11), null, now), patientId, providerId);
            Assert.True(await appointments.TryUpdateStatusAsync(confirmedId, AppointmentStatus.REQUESTED, AppointmentStatus.CONFIRMED));
            Assert.True(await appointments.TryUpdateStatusAsync(declinedId, AppointmentStatus.REQUESTED, AppointmentStatus.DECLINED));

            // With "now" one day before the far day, the upcoming list starts with the seeded appointments.
            var dashboard = await repository.GetDashboardAsync(upcomingDay.AddDays(-1));
            Assert.True(dashboard.Clinics >= before.Clinics + 1);
            Assert.True(dashboard.Providers >= before.Providers + 1);
            Assert.True(dashboard.Patients >= before.Patients + 1);
            Assert.True(dashboard.Appointments >= before.Appointments + 3);

            Assert.Equal(
                new[] { requestedId, confirmedId },
                dashboard.UpcomingAppointments.Take(2).Select(result => result.Id));
            Assert.DoesNotContain(dashboard.UpcomingAppointments, result => result.Id == declinedId);
            Assert.True(dashboard.UpcomingAppointments.Count <= 5);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Appointments WHERE ProviderId IN :Ids", [providerId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Providers WHERE Id IN :Ids", [providerId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Patients WHERE Id IN :Ids", [patientId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [clinicId]);
        }
    }

    [Fact]
    public async Task StatusRepository_PlatformCountsExcludeDeclinedAndCountPendingVerificationInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var appointments = new AppointmentRepository(connectionFactory);
        var repository = new StatusRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var verifiedProviderId = Guid.NewGuid();
        var pendingProviderId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var requestedId = Guid.NewGuid();
        var declinedId = Guid.NewGuid();
        var todayStart = FarDay;
        var todayEnd = FarDay.AddDays(1);
        var now = DateTimeOffset.UtcNow;

        var before = await repository.GetPlatformCountsAsync(todayStart, todayEnd);
        try
        {
            var clinic = Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101");
            await clinics.AddClinicAsync(clinic);
            await providers.AddProviderAsync(Provider.Create(verifiedProviderId, clinicId, "Grace", "Hopper", "General Practice"), clinicId);

            // A pending provider: self-registered at the clinic, not yet verified.
            await providers.AddProviderAsync(Provider.Register(pendingProviderId, clinicId, "Alan", "Turing", "Dermatology"), clinicId);

            await patients.AddPatientAsync(Patient.Create(patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), $"ada.{patientId:N}@example.com", "555-0100"));
            await appointments.AddAppointmentAsync(
                Appointment.Create(requestedId, patientId, verifiedProviderId, todayStart.AddHours(9), null, now),
                patientId,
                verifiedProviderId);
            await appointments.AddAppointmentAsync(
                Appointment.Create(declinedId, patientId, verifiedProviderId, todayStart.AddHours(10), null, now),
                patientId,
                verifiedProviderId);
            Assert.True(await appointments.TryUpdateStatusAsync(declinedId, AppointmentStatus.REQUESTED, AppointmentStatus.DECLINED));

            var after = await repository.GetPlatformCountsAsync(todayStart, todayEnd);
            // The far-day window holds no other rows, so the declined appointment is the only thing left out.
            Assert.Equal(before.TodayAppointmentCount + 1, after.TodayAppointmentCount);
            Assert.True(after.PendingProviderVerificationCount >= before.PendingProviderVerificationCount + 1);
            Assert.True(after.ProviderCount >= before.ProviderCount + 2);
            Assert.True(after.PatientCount >= before.PatientCount + 1);
            Assert.True(after.ClinicCount >= before.ClinicCount + 1);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Appointments WHERE ProviderId IN :Ids", [verifiedProviderId, pendingProviderId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Providers WHERE Id IN :Ids", [verifiedProviderId, pendingProviderId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Patients WHERE Id IN :Ids", [patientId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [clinicId]);
        }
    }

    [Fact]
    public async Task StatusRepository_ClinicCountsAreScopedToGivenClinicsInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var appointments = new AppointmentRepository(connectionFactory);
        var repository = new StatusRepository(connectionFactory);
        var clinicAId = Guid.NewGuid();
        var clinicBId = Guid.NewGuid();
        var clinicCId = Guid.NewGuid();
        var providerA1Id = Guid.NewGuid();
        var providerA2PendingId = Guid.NewGuid();
        var providerBId = Guid.NewGuid();
        var providerCId = Guid.NewGuid();
        var patient1Id = Guid.NewGuid();
        var patient2Id = Guid.NewGuid();
        var patient3Id = Guid.NewGuid();
        var appointmentIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(2090, 6, 10, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        try
        {
            var clinicA = Clinic.Create(clinicAId, $"Clinic A {clinicAId:N}", "1 Test Street", "555-0101");
            var clinicB = Clinic.Create(clinicBId, $"Clinic B {clinicBId:N}", "2 Test Street", "555-0102");
            var clinicC = Clinic.Create(clinicCId, $"Clinic C {clinicCId:N}", "3 Test Street", "555-0103");
            await clinics.AddClinicAsync(clinicA);
            await clinics.AddClinicAsync(clinicB);
            await clinics.AddClinicAsync(clinicC);

            await providers.AddProviderAsync(Provider.Create(providerA1Id, clinicAId, "Grace", "Hopper", "General Practice"), clinicAId);
            await providers.AddProviderAsync(Provider.Register(providerA2PendingId, clinicAId, "Alan", "Turing", "Dermatology"), clinicAId);
            await providers.AddProviderAsync(Provider.Create(providerBId, clinicBId, "Ada", "Lovelace", "Neurology"), clinicBId);
            await providers.AddProviderAsync(Provider.Create(providerCId, clinicCId, "Edsger", "Dijkstra", "Cardiology"), clinicCId);

            await patients.AddPatientAsync(Patient.Create(patient1Id, "Pat", "One", new DateOnly(1980, 1, 1), $"p1.{patient1Id:N}@example.com", "555-0201"));
            await patients.AddPatientAsync(Patient.Create(patient2Id, "Pat", "Two", new DateOnly(1981, 1, 1), $"p2.{patient2Id:N}@example.com", "555-0202"));
            await patients.AddPatientAsync(Patient.Create(patient3Id, "Pat", "Three", new DateOnly(1982, 1, 1), $"p3.{patient3Id:N}@example.com", "555-0203"));

            // In window and counted: a1, a2, a3. Declined in window: a4. Outside the window: a5. Outside the clinics: a6.
            var seeds = new (Guid Patient, Guid Provider, DateTimeOffset When, AppointmentStatus Status)[]
            {
                (patient1Id, providerA1Id, todayStart.AddHours(9), AppointmentStatus.REQUESTED),
                (patient2Id, providerA2PendingId, todayStart.AddHours(10), AppointmentStatus.CONFIRMED),
                (patient2Id, providerBId, todayStart.AddHours(11), AppointmentStatus.REQUESTED),
                (patient1Id, providerA1Id, todayStart.AddHours(12), AppointmentStatus.DECLINED),
                (patient1Id, providerA1Id, todayEnd.AddHours(9), AppointmentStatus.REQUESTED),
                (patient3Id, providerCId, todayStart.AddHours(9), AppointmentStatus.REQUESTED),
            };
            for (var i = 0; i < seeds.Length; i++)
            {
                var seed = seeds[i];
                await appointments.AddAppointmentAsync(
                    Appointment.Create(appointmentIds[i], seed.Patient, seed.Provider, seed.When, null, now),
                    seed.Patient,
                    seed.Provider);
                if (seed.Status != AppointmentStatus.REQUESTED)
                {
                    Assert.True(await appointments.TryUpdateStatusAsync(
                        appointmentIds[i], AppointmentStatus.REQUESTED, seed.Status));
                }
            }

            var counts = await repository.GetClinicCountsAsync(
                new[] { clinicAId, clinicBId, clinicAId },
                todayStart,
                todayEnd);

            Assert.Equal(2, counts.ClinicCount);
            Assert.Equal(3, counts.ProviderCount);
            Assert.Equal(2, counts.PatientCount);
            Assert.Equal(1, counts.PendingProviderVerificationCount);
            Assert.Equal(3, counts.TodayAppointmentCount);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Appointments WHERE ProviderId IN :Ids",
                [providerA1Id, providerA2PendingId, providerBId, providerCId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Providers WHERE Id IN :Ids",
                [providerA1Id, providerA2PendingId, providerBId, providerCId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Patients WHERE Id IN :Ids", [patient1Id, patient2Id, patient3Id]);
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [clinicAId, clinicBId, clinicCId]);
        }
    }

    [Fact]
    public async Task StatusRepository_DoctorDashboardCountsOnlyTheProviderAppointmentsInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var appointments = new AppointmentRepository(connectionFactory);
        var repository = new StatusRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var otherProviderId = Guid.NewGuid();
        var patientOneId = Guid.NewGuid();
        var patientTwoId = Guid.NewGuid();
        var todayRequestedId = Guid.NewGuid();
        var todayConfirmedId = Guid.NewGuid();
        var todayDeclinedId = Guid.NewGuid();
        var weekRequestedId = Guid.NewGuid();
        var laterConfirmedId = Guid.NewGuid();
        var otherProviderAppointmentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var doctorDay = new DateTimeOffset(2090, 9, 10, 0, 0, 0, TimeSpan.Zero);
        var todayStart = doctorDay;
        var todayEnd = doctorDay.AddDays(1);
        var weekEnd = doctorDay.AddDays(7);

        try
        {
            await clinics.AddClinicAsync(Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101"));
            await providers.AddProviderAsync(Provider.Create(providerId, clinicId, "Grace", "Hopper", "General Practice"), clinicId);
            await providers.AddProviderAsync(Provider.Create(otherProviderId, clinicId, "Alan", "Turing", "Neurology"), clinicId);
            await patients.AddPatientAsync(Patient.Create(patientOneId, "Pat", "One", new DateOnly(1980, 1, 1), $"p1.{patientOneId:N}@example.com", "555-0201"));
            await patients.AddPatientAsync(Patient.Create(patientTwoId, "Pat", "Two", new DateOnly(1981, 1, 1), $"p2.{patientTwoId:N}@example.com", "555-0202"));

            var seeds = new (Guid Id, Guid Provider, Guid Patient, DateTimeOffset When, AppointmentStatus Status)[]
            {
                (todayRequestedId, providerId, patientOneId, todayStart.AddHours(9), AppointmentStatus.REQUESTED),
                (todayConfirmedId, providerId, patientOneId, todayStart.AddHours(15), AppointmentStatus.CONFIRMED),
                (todayDeclinedId, providerId, patientTwoId, todayStart.AddHours(10), AppointmentStatus.DECLINED),
                (weekRequestedId, providerId, patientTwoId, todayStart.AddDays(3).AddHours(10), AppointmentStatus.REQUESTED),
                (laterConfirmedId, providerId, patientOneId, todayStart.AddDays(10), AppointmentStatus.CONFIRMED),
                (otherProviderAppointmentId, otherProviderId, patientTwoId, todayStart.AddHours(9), AppointmentStatus.REQUESTED),
            };
            foreach (var seed in seeds)
            {
                await appointments.AddAppointmentAsync(
                    Appointment.Create(seed.Id, seed.Patient, seed.Provider, seed.When, null, now),
                    seed.Patient,
                    seed.Provider);
                if (seed.Status != AppointmentStatus.REQUESTED)
                {
                    Assert.True(await appointments.TryUpdateStatusAsync(seed.Id, AppointmentStatus.REQUESTED, seed.Status));
                }
            }

            var section = await repository.GetDoctorDashboardAsync(
                providerId,
                todayStart.AddDays(-1),
                todayStart,
                todayEnd,
                weekEnd);

            // Today: the requested and confirmed ones (declined excluded). Week adds the day-three request.
            Assert.Equal(2, section.TodayAppointmentCount);
            Assert.Equal(3, section.WeekAppointmentCount);
            // Distinct patients over all of this provider's appointments, including the declined one.
            Assert.Equal(2, section.DistinctPatientCount);
            // Requested appointments starting after "now": today's request and the day-three request.
            Assert.Equal(2, section.PendingRequestCount);
            Assert.NotNull(section.NextAppointment);
            Assert.Equal(todayRequestedId, section.NextAppointment.Id);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Appointments WHERE ProviderId IN :Ids", [providerId, otherProviderId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Providers WHERE Id IN :Ids", [providerId, otherProviderId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Patients WHERE Id IN :Ids", [patientOneId, patientTwoId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [clinicId]);
        }
    }

    [Fact]
    public async Task StatusRepository_PatientDashboardShowsUpcomingAndRecentDecisionsInOracle()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();

        var connectionFactory = new OracleConnectionFactory(connectionString!);
        var clinics = new ClinicRepository(connectionFactory);
        var providers = new ProviderRepository(connectionFactory);
        var patients = new PatientRepository(connectionFactory);
        var appointments = new AppointmentRepository(connectionFactory);
        var repository = new StatusRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var confirmedSoonId = Guid.NewGuid();
        var requestedLaterId = Guid.NewGuid();
        var declinedId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        try
        {
            await clinics.AddClinicAsync(Clinic.Create(clinicId, $"Clinic {clinicId:N}", "1 Test Street", "555-0101"));
            await providers.AddProviderAsync(Provider.Create(providerId, clinicId, "Grace", "Hopper", "General Practice"), clinicId);
            await patients.AddPatientAsync(Patient.Create(patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), $"ada.{patientId:N}@example.com", "555-0100"));

            await appointments.AddAppointmentAsync(
                Appointment.Create(confirmedSoonId, patientId, providerId, now.AddDays(2), null, now), patientId, providerId);
            await appointments.AddAppointmentAsync(
                Appointment.Create(requestedLaterId, patientId, providerId, now.AddDays(3), null, now), patientId, providerId);
            await appointments.AddAppointmentAsync(
                Appointment.Create(declinedId, patientId, providerId, now.AddDays(1), null, now), patientId, providerId);

            // Confirmed first, declined second, so the declined one is the most recent decision.
            Assert.True(await appointments.TryUpdateStatusAsync(confirmedSoonId, AppointmentStatus.REQUESTED, AppointmentStatus.CONFIRMED));
            Assert.True(await appointments.TryUpdateStatusAsync(declinedId, AppointmentStatus.REQUESTED, AppointmentStatus.DECLINED));

            var section = await repository.GetPatientDashboardAsync(patientId, now);

            // Upcoming: confirmed and requested ones; the declined one is not upcoming.
            Assert.Equal(2, section.UpcomingAppointmentCount);
            Assert.NotNull(section.NextAppointment);
            Assert.Equal(confirmedSoonId, section.NextAppointment.Id);

            Assert.Equal(
                new[] { declinedId, confirmedSoonId },
                section.RecentDecisions.Select(decision => decision.AppointmentId));
            Assert.Equal("CONFIRMED", section.RecentDecisions[1].Status);
            Assert.Equal("DECLINED", section.RecentDecisions[0].Status);
            Assert.All(section.RecentDecisions, decision => Assert.Equal("Grace Hopper", decision.ProviderName));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await DeleteWhereInAsync(connection, "DELETE FROM Appointments WHERE ProviderId IN :Ids", [providerId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Providers WHERE Id IN :Ids", [providerId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Patients WHERE Id IN :Ids", [patientId]);
            await DeleteWhereInAsync(connection, "DELETE FROM Clinics WHERE Id IN :Ids", [clinicId]);
        }
    }

    private static async Task DeleteWhereInAsync(IDbConnection connection, string sql, Guid[] ids)
    {
        await connection.ExecuteAsync(sql, new { Ids = ids.Select(id => id.ToString()).ToArray() });
    }
}
