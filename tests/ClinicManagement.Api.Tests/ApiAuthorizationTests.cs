using System.Net;
using System.Net.Http.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests;

public sealed class ApiAuthorizationTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    private static readonly string[] NonAdministrativeRoles = ["patient", "doctor", "clinicadmin"];

    [Fact]
    public async Task ProtectedRoutes_ReturnUnauthorizedWithoutToken()
    {
        var world = SeedWorld();
        var routes = new (HttpMethod Method, string Url)[]
        {
            (HttpMethod.Get, "/api/me"),
            (HttpMethod.Get, "/api/clinics"),
            (HttpMethod.Post, "/api/clinics"),
            (HttpMethod.Put, $"/api/clinics/{world.Clinic.Id}"),
            (HttpMethod.Delete, $"/api/clinics/{world.Clinic.Id}"),
            (HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/appointments"),
            (HttpMethod.Get, "/api/appointments"),
            (HttpMethod.Post, "/api/appointments"),
            (HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}"),
            (HttpMethod.Post, $"/api/appointments/{world.Appointment.Id}/confirm"),
            (HttpMethod.Post, $"/api/providers/{world.PendingProvider.Id}/verify"),
            (HttpMethod.Post, $"/api/providers/{world.PendingProvider.Id}/reject"),
            (HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/calendar?{CalendarQuery}"),
            (HttpMethod.Get, $"/api/patients/{world.Patient.Id}/appointments"),
            (HttpMethod.Put, $"/api/patients/{world.Patient.Id}"),
            (HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/approve"),
            (HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/reject"),
            (HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/join-requests")
        };

        foreach (var (method, url) in routes)
        {
            using var response = await SendAsync(method, url, subject: null, body: method == HttpMethod.Get || method == HttpMethod.Delete ? null : new { });

            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"{method} {url} returned {(int)response.StatusCode}, expected 401.");
        }
    }

    [Fact]
    public async Task ProviderDirectory_IsPublicToAnonymousCallers()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/providers/directory", subject: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsRolesDerivedFromStoredRecords()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me", world.DoctorSubject);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"doctor\"", body);
        Assert.DoesNotContain("\"admin\"", body);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task ClinicCrud_IsAdministratorOnly(string role)
    {
        var world = SeedWorld();
        var subject = world.SubjectFor(role);
        var clinicRequest = new ClinicRequest(Guid.Empty, "New Clinic", "3 Lake Road", "555-0199");

        using var create = await SendAsync(HttpMethod.Post, "/api/clinics", subject, clinicRequest);
        using var update = await SendAsync(HttpMethod.Put, $"/api/clinics/{world.Clinic.Id}", subject, clinicRequest);
        using var delete = await SendAsync(HttpMethod.Delete, $"/api/clinics/{world.Clinic.Id}", subject);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Contains(world.Clinic, Factory.Store.Clinics);
    }

    [Fact]
    public async Task ClinicCrud_AdministratorCanCreateClinic()
    {
        var world = SeedWorld();
        var clinicRequest = new ClinicRequest(Guid.Empty, "New Clinic", "3 Lake Road", "555-0199");

        using var response = await SendAsync(HttpMethod.Post, "/api/clinics", world.SubjectFor("admin"), clinicRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(Factory.Store.Clinics, clinic => clinic.Name == "New Clinic");
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task ProviderVerification_IsAdministratorOnly(string role)
    {
        var world = SeedWorld();
        var subject = world.SubjectFor(role);

        using var verify = await SendAsync(HttpMethod.Post, $"/api/providers/{world.PendingProvider.Id}/verify", subject);
        using var reject = await SendAsync(HttpMethod.Post, $"/api/providers/{world.PendingProvider.Id}/reject", subject);

        Assert.Equal(HttpStatusCode.Forbidden, verify.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
        Assert.Equal(ProviderVerificationStatus.PENDING, world.PendingProvider.VerificationStatus);
    }

    [Fact]
    public async Task ProviderVerification_AdministratorCanVerifyPendingProvider()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/providers/{world.PendingProvider.Id}/verify",
            world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ProviderVerificationStatus.VERIFIED, world.PendingProvider.VerificationStatus);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    public async Task JoinRequestDecisions_RequireClinicAdminRole(string role)
    {
        var world = SeedWorld();
        var subject = world.SubjectFor(role);

        using var approve = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/approve", subject);
        using var reject = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/reject", subject);

        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
    }

    [Fact]
    public async Task JoinRequestDecisions_ForbidClinicAdminOfAnotherClinic()
    {
        var world = SeedWorld();
        var subject = world.OtherClinicAdminSubject;

        using var approve = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/approve", subject);
        using var reject = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/reject", subject);

        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
    }

    [Fact]
    public async Task JoinRequestApproval_AllowsClinicAdminOfThatClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/clinic-join-requests/{world.JoinRequest.Id}/approve",
            world.ClinicAdminSubject);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Calendar_AllowsTheVerifiedProviderItself()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{world.Doctor.Id}/calendar?{CalendarQuery}",
            world.DoctorSubject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Calendar_ForbidsAnotherDoctor()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{world.Doctor.Id}/calendar?{CalendarQuery}",
            world.OtherDoctorSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Calendar_ForbidsUnverifiedProvider()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{world.PendingProvider.Id}/calendar?{CalendarQuery}",
            world.PendingSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("clinicadmin")]
    public async Task Calendar_IsDoctorOnly(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{world.Doctor.Id}/calendar?{CalendarQuery}",
            world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AppointmentConfirmation_AllowsTheAppointmentsDoctor()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/appointments/{world.Appointment.Id}/confirm",
            world.DoctorSubject);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AppointmentConfirmation_ForbidsAnotherDoctor()
    {
        var world = SeedWorld();

        using var confirm = await SendAsync(HttpMethod.Post, $"/api/appointments/{world.Appointment.Id}/confirm", world.OtherDoctorSubject);
        using var decline = await SendAsync(HttpMethod.Post, $"/api/appointments/{world.Appointment.Id}/decline", world.OtherDoctorSubject);

        Assert.Equal(HttpStatusCode.Forbidden, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, decline.StatusCode);
    }

    [Fact]
    public async Task AppointmentDecline_ForbidsUnverifiedProvider()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/appointments/{world.Appointment.Id}/decline",
            world.PendingSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("clinicadmin")]
    public async Task AppointmentConfirmation_IsDoctorOnly(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/appointments/{world.Appointment.Id}/confirm",
            world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task Booking_IsPatientOnly(string role)
    {
        var world = SeedWorld();
        var request = new AppointmentRequest(Guid.NewGuid(), world.Patient.Id, world.Doctor.Id, Booking, "Checkup");

        using var response = await SendAsync(HttpMethod.Post, "/api/appointments", world.SubjectFor(role), request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain(Factory.Store.Appointments, appointment => appointment.Id == request.Id);
    }

    [Fact]
    public async Task Booking_ForcesTheCallersPatientIdRegardlessOfRequestBody()
    {
        var world = SeedWorld();
        var request = new AppointmentRequest(Guid.NewGuid(), world.OtherPatient.Id, world.Doctor.Id, Booking, "Checkup");

        using var response = await SendAsync(HttpMethod.Post, "/api/appointments", world.PatientSubject, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var booked = Factory.Store.Appointments.Single(appointment => appointment.Id == created!["id"]);
        Assert.Equal(world.Patient.Id, booked.PatientId);
    }

    [Fact]
    public async Task PatientAppointments_AllowsTheOwningPatient()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/patients/{world.Patient.Id}/appointments",
            world.PatientSubject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatientAppointments_ForbidsAnotherPatient()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/patients/{world.Patient.Id}/appointments",
            world.OtherPatientSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task PatientAppointments_ForbidStaffWithoutPatientProfile(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/patients/{world.Patient.Id}/appointments",
            world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatientProfile_UpdateForbidsAnotherPatient()
    {
        var world = SeedWorld();
        var request = new PatientRequest(world.Patient.Id, "Mallory", "Attacker", new DateOnly(1990, 1, 1), "555-0666", "mallory@example.com");

        using var response = await SendAsync(
            HttpMethod.Put,
            $"/api/patients/{world.Patient.Id}",
            world.OtherPatientSubject,
            request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Ada", world.Patient.FirstName);
    }

    [Fact]
    public async Task PatientProfile_UpdateAllowsTheOwningPatient()
    {
        var world = SeedWorld();
        var request = new PatientRequest(world.Patient.Id, "Ada", "King", new DateOnly(1985, 12, 10), "555-0102", "ada@example.com");

        using var response = await SendAsync(
            HttpMethod.Put,
            $"/api/patients/{world.Patient.Id}",
            world.PatientSubject,
            request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task PatientProfile_UpdateIsPatientOnly(string role)
    {
        var world = SeedWorld();
        var request = new PatientRequest(world.Patient.Id, "Ada", "King", new DateOnly(1985, 12, 10), "555-0102", "ada@example.com");

        using var response = await SendAsync(
            HttpMethod.Put,
            $"/api/patients/{world.Patient.Id}",
            world.SubjectFor(role),
            request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AppointmentDetail_AllowsParticipants()
    {
        var world = SeedWorld();

        using var patientView = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}", world.PatientSubject);
        using var doctorView = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}", world.DoctorSubject);

        Assert.Equal(HttpStatusCode.OK, patientView.StatusCode);
        Assert.Equal(HttpStatusCode.OK, doctorView.StatusCode);
    }

    [Fact]
    public async Task AppointmentDetail_AllowsClinicAdminOfTheProvidersClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/appointments/{world.Appointment.Id}",
            world.ClinicAdminSubject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AppointmentDetail_ForbidsUnrelatedUsers()
    {
        var world = SeedWorld();

        using var otherPatient = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}", world.OtherPatientSubject);
        using var otherDoctor = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}", world.OtherDoctorSubject);
        using var otherClinicAdmin = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}", world.OtherClinicAdminSubject);

        Assert.Equal(HttpStatusCode.Forbidden, otherPatient.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherDoctor.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherClinicAdmin.StatusCode);
    }

    [Fact]
    public async Task AppointmentDetail_AllowsAdministrator()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/appointments/{world.Appointment.Id}",
            world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ClinicAppointments_AllowsClinicAdminOfThatClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/clinics/{world.Clinic.Id}/appointments",
            world.ClinicAdminSubject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ClinicAppointments_ForbidsClinicAdminOfAnotherClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/clinics/{world.Clinic.Id}/appointments",
            world.OtherClinicAdminSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task AllAppointments_IsAdministratorOnly(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/appointments", world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AllAppointments_AdministratorCanList()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/appointments", world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
