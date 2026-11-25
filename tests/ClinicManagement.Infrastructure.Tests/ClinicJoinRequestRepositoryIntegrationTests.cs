using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Infrastructure.Repositories;
using Dapper;
using Infrastructure.Data;

namespace ClinicManagement.Infrastructure.Tests;

public class ClinicJoinRequestRepositoryIntegrationTests
{
    [Fact]
    public async Task ProviderRegistrationAndJoinWorkflow_WorksEndToEndInOracle()
    {
        var connectionFactory = CreateConnectionFactory();
        var clinicRepository = new ClinicRepository(connectionFactory);
        var providerRepository = new ProviderRepository(connectionFactory);
        var requestRepository = new ClinicJoinRequestRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var rejectedProviderId = Guid.NewGuid();
        var marker = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var homeClinicId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicId, $"Clinic {marker}", "1 Test Street", "555-0101");
        var homeClinic = Clinic.Create(homeClinicId, $"Home {marker}", "2 Test Street", "555-0102");
        var provider = Provider.Register(providerId, homeClinicId, "Grace", marker, "Cardiology");
        var rejectedProvider = Provider.Register(rejectedProviderId, homeClinicId, "Alan", marker, "Cardiology");
        var everyone = new PageQuery(Search: marker, PageSize: 50);

        try
        {
            await clinicRepository.AddClinicAsync(clinic);
            await clinicRepository.AddClinicAsync(homeClinic);
            await providerRepository.AddProviderAsync(provider, homeClinicId);
            await providerRepository.AddProviderAsync(rejectedProvider, homeClinicId);

            // A self-registered provider is pending at the clinic they registered with.
            var stored = await providerRepository.GetProviderByIdAsync(providerId);
            Assert.NotNull(stored);
            Assert.Equal(ProviderVerificationStatus.PENDING, stored.VerificationStatus);
            Assert.Equal(homeClinicId, stored.ClinicId);
            Assert.Equal(homeClinic.Name, stored.Clinic!.Name);

            // Management search sees them (and can filter on status); the public directory does not.
            var (pending, _) = await providerRepository.SearchProvidersAsync(
                everyone, 0, null, ProviderVerificationStatus.PENDING);
            Assert.Equal(2, pending.Count);
            Assert.Empty((await providerRepository.SearchProvidersAsync(
                everyone, 0, null, ProviderVerificationStatus.VERIFIED)).Items);
            Assert.Empty((await providerRepository.SearchPublicProvidersAsync(everyone, 0, null)).Items);
            Assert.Contains(await providerRepository.GetAllProvidersAsync(), p => p.Id == providerId);

            // Verification is a compare-and-set on the stored status.
            Assert.True(await providerRepository.TryUpdateVerificationStatusAsync(
                providerId, ProviderVerificationStatus.PENDING, ProviderVerificationStatus.VERIFIED));
            Assert.False(await providerRepository.TryUpdateVerificationStatusAsync(
                providerId, ProviderVerificationStatus.PENDING, ProviderVerificationStatus.REJECTED));
            Assert.Equal(providerId, Assert.Single((await providerRepository.SearchPublicProvidersAsync(everyone, 0, null)).Items).Id);

            // One pending join request per provider, enforced by the unique index too.
            var request = ClinicJoinRequest.Create(Guid.NewGuid(), providerId, clinicId);
            Assert.False(await requestRepository.HasPendingAsync(providerId));
            await requestRepository.AddAsync(request);
            Assert.True(await requestRepository.HasPendingAsync(providerId));
            await Assert.ThrowsAsync<ClinicJoinRequestAlreadyPendingException>(
                () => requestRepository.AddAsync(
                    ClinicJoinRequest.Create(Guid.NewGuid(), providerId, clinicId)));

            var pendingForClinic = await requestRepository.GetByClinicIdAsync(clinicId, ClinicJoinRequestStatus.PENDING);
            var listed = Assert.Single(pendingForClinic);
            Assert.Equal($"Grace {marker}", listed.ProviderName);
            Assert.Equal(clinic.Name, listed.ClinicName);
            Assert.Equal("PENDING", listed.Status);
            Assert.Empty(await requestRepository.GetByClinicIdAsync(clinicId, ClinicJoinRequestStatus.APPROVED));
            Assert.Single(await requestRepository.GetByProviderIdAsync(providerId));

            // Approval flips the request and moves the provider to the clinic in one go...
            var toApprove = await requestRepository.GetByIdAsync(request.Id);
            Assert.NotNull(toApprove);
            toApprove.Approve(now);
            Assert.True(await requestRepository.TryApproveAsync(toApprove));

            var approved = await requestRepository.GetByIdAsync(request.Id);
            Assert.Equal(ClinicJoinRequestStatus.APPROVED, approved!.Status);
            Assert.NotNull(approved.DecidedAtUtc);
            var assigned = await providerRepository.GetProviderByIdAsync(providerId);
            Assert.Equal(clinicId, assigned!.ClinicId);
            Assert.Equal(clinic.Name, assigned.Clinic!.Name);
            Assert.Equal(providerId, Assert.Single((await providerRepository.SearchPublicProvidersAsync(everyone, 0, null)).Items).Id);
            Assert.Equal(providerId, Assert.Single(await providerRepository.GetProvidersByClinicIdAsync(clinicId)).Id);

            // ...and cannot be applied twice.
            Assert.False(await requestRepository.TryApproveAsync(toApprove));
            Assert.False(await requestRepository.TryDecideAsync(toApprove));

            // The provider is already at that clinic, so approving another request for it rolls back entirely.
            var second = ClinicJoinRequest.Create(Guid.NewGuid(), providerId, clinicId);
            await requestRepository.AddAsync(second);
            second.Approve(now);
            Assert.False(await requestRepository.TryApproveAsync(second));
            Assert.Equal(
                ClinicJoinRequestStatus.PENDING,
                (await requestRepository.GetByIdAsync(second.Id))!.Status);
            Assert.Equal(clinicId, (await providerRepository.GetProviderByIdAsync(providerId))!.ClinicId);

            // Rejection path.
            await providerRepository.TryUpdateVerificationStatusAsync(
                rejectedProviderId, ProviderVerificationStatus.PENDING, ProviderVerificationStatus.VERIFIED);
            var toReject = ClinicJoinRequest.Create(Guid.NewGuid(), rejectedProviderId, clinicId);
            await requestRepository.AddAsync(toReject);
            toReject.Reject(now);
            Assert.True(await requestRepository.TryDecideAsync(toReject));
            Assert.Equal(homeClinicId, (await providerRepository.GetProviderByIdAsync(rejectedProviderId))!.ClinicId);
            Assert.False(await requestRepository.HasPendingAsync(rejectedProviderId));

            // Join requests disappear with the provider and the clinic they belong to.
            await providerRepository.DeleteProviderAsync(rejectedProviderId);
            Assert.Null(await requestRepository.GetByIdAsync(toReject.Id));
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync(
                "DELETE FROM Providers WHERE LastName = :Marker", new { Marker = marker });
            await connection.ExecuteAsync(
                "DELETE FROM Clinics WHERE Name IN (:First, :Home)",
                new { First = $"Clinic {marker}", Home = $"Home {marker}" });
        }
    }

    [Fact]
    public async Task AppointmentStatusAndCalendar_WorkInOracle()
    {
        var connectionFactory = CreateConnectionFactory();
        var clinicRepository = new ClinicRepository(connectionFactory);
        var patientRepository = new PatientRepository(connectionFactory);
        var providerRepository = new ProviderRepository(connectionFactory);
        var repository = new AppointmentRepository(connectionFactory);
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var slot = new DateTimeOffset(now.Year, now.Month, now.Day, 10, 0, 0, TimeSpan.Zero).AddDays(5);
        var appointment = Appointment.Create(Guid.NewGuid(), patientId, providerId, slot, "Checkup", now);
        var later = Appointment.Create(Guid.NewGuid(), patientId, providerId, slot.AddDays(30), "Later", now);

        try
        {
            await clinicRepository.AddClinicAsync(Clinic.Create(clinicId, $"Clinic {clinicId:N}", null, null));
            await patientRepository.AddPatientAsync(Patient.Create(
                patientId, "Ada", "Lovelace", new DateOnly(1985, 12, 10), $"ada.{patientId:N}@example.com", "555-0100"));
            await providerRepository.AddProviderAsync(
                Provider.Create(providerId, clinicId, "Grace", "Hopper", "General"), clinicId);
            await repository.AddAppointmentAsync(appointment, patientId, providerId);
            await repository.AddAppointmentAsync(later, patientId, providerId);

            // New bookings are stored as REQUESTED, and a requested slot is already taken.
            Assert.Equal(AppointmentStatus.REQUESTED, (await repository.GetAppointmentByIdAsync(appointment.Id))!.Status);
            Assert.True(await repository.HasProviderAppointmentOverlapAsync(
                providerId, slot.AddMinutes(-10), slot.AddMinutes(10), null));

            // Status changes are compare-and-set.
            Assert.False(await repository.TryUpdateStatusAsync(
                appointment.Id, AppointmentStatus.CONFIRMED, AppointmentStatus.COMPLETED));
            Assert.True(await repository.TryUpdateStatusAsync(
                appointment.Id, AppointmentStatus.REQUESTED, AppointmentStatus.CONFIRMED));
            Assert.Equal(AppointmentStatus.CONFIRMED, (await repository.GetAppointmentByIdAsync(appointment.Id))!.Status);

            // The calendar covers [start, end) and carries the status.
            var week = await repository.GetProviderAppointmentsInRangeAsync(
                providerId, slot.AddDays(-1), slot.AddDays(7), includeInactive: false);
            var item = Assert.Single(week);
            Assert.Equal(appointment.Id, item.Id);
            Assert.Equal("CONFIRMED", item.Status);
            Assert.Equal($"Clinic {clinicId:N}", item.ClinicName);
            Assert.Empty(await repository.GetProviderAppointmentsInRangeAsync(
                providerId, slot.AddDays(-1), slot, includeInactive: false));

            // Cancelling frees the slot and hides the appointment unless inactive ones are requested.
            Assert.True(await repository.TryUpdateStatusAsync(
                appointment.Id, AppointmentStatus.CONFIRMED, AppointmentStatus.CANCELLED));
            Assert.False(await repository.HasProviderAppointmentOverlapAsync(
                providerId, slot.AddMinutes(-10), slot.AddMinutes(10), null));
            Assert.Empty(await repository.GetProviderAppointmentsInRangeAsync(
                providerId, slot.AddDays(-1), slot.AddDays(7), includeInactive: false));
            Assert.Single(await repository.GetProviderAppointmentsInRangeAsync(
                providerId, slot.AddDays(-1), slot.AddDays(7), includeInactive: true));

            // Search can filter by status, and the upcoming list skips inactive appointments.
            var (cancelled, cancelledCount) = await repository.SearchAppointmentsAsync(
                new AppointmentSearchQuery(ProviderId: providerId, Status: AppointmentStatus.CANCELLED), 0);
            Assert.Equal(1, cancelledCount);
            Assert.Equal(appointment.Id, Assert.Single(cancelled).Id);
            var upcoming = await repository.GetUpcomingAppointmentsAsync(now, 1000);
            Assert.DoesNotContain(upcoming, a => a.Id == appointment.Id);
            Assert.Contains(upcoming, a => a.Id == later.Id);
        }
        finally
        {
            using var connection = connectionFactory.CreateConnection();
            await connection.ExecuteAsync("DELETE FROM Appointments WHERE ProviderId = :Id", new { Id = providerId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Providers WHERE Id = :Id", new { Id = providerId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Clinics WHERE Id = :Id", new { Id = clinicId.ToString() });
            await connection.ExecuteAsync("DELETE FROM Patients WHERE Id = :Id", new { Id = patientId.ToString() });
        }
    }

    private static OracleConnectionFactory CreateConnectionFactory()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set TEST_DATABASE_CONNECTION_STRING to an Oracle test database.");

        OracleDapperTypeHandlers.Register();
        return new OracleConnectionFactory(connectionString!);
    }
}
