using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Domain.Entities;
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
            (HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/summary"),
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
    public async Task ProviderDirectory_ExcludesUnverifiedProviders()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/providers/directory", subject: null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(world.Doctor.Id.ToString(), body);
        Assert.DoesNotContain(world.PendingProvider.Id.ToString(), body);
    }

    [Fact]
    public async Task ProviderSearch_IsPublicAndExcludesUnverifiedProvidersForAnonymousCallers()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/providers/search?page=1&pageSize=50", subject: null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(world.Doctor.Id.ToString(), body);
        Assert.DoesNotContain(world.PendingProvider.Id.ToString(), body);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task ProviderSearch_ExcludesUnverifiedProvidersForNonAdministrators(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            "/api/providers/search?page=1&pageSize=50&verificationStatus=PENDING",
            world.SubjectFor(role));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(world.PendingProvider.Id.ToString(), body);
    }

    [Fact]
    public async Task ProviderSearch_AdministratorSeesUnverifiedProviders()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/providers/search?page=1&pageSize=50", world.SubjectFor("admin"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(world.PendingProvider.Id.ToString(), body);
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

    [Fact]
    public async Task AllergyTypeReads_RequireSignIn()
    {
        var allergyType = AddAllergyType();

        using var list = await SendAsync(HttpMethod.Get, "/api/allergy-types", subject: null);
        using var search = await SendAsync(HttpMethod.Get, "/api/allergy-types/search?page=1&pageSize=10", subject: null);
        using var detail = await SendAsync(HttpMethod.Get, $"/api/allergy-types/{allergyType.Id}", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, search.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detail.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task AllergyTypeReads_AllowAnySignedInUser(string role)
    {
        var world = SeedWorld();
        var allergyType = AddAllergyType();

        using var list = await SendAsync(HttpMethod.Get, "/api/allergy-types", world.SubjectFor(role));
        using var detail = await SendAsync(HttpMethod.Get, $"/api/allergy-types/{allergyType.Id}", world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task AllergyTypeWrites_RequireSignIn()
    {
        var allergyType = AddAllergyType();
        var request = new AllergyTypeRequest(Guid.Empty, "Latex");

        using var create = await SendAsync(HttpMethod.Post, "/api/allergy-types", subject: null, request);
        using var update = await SendAsync(HttpMethod.Put, $"/api/allergy-types/{allergyType.Id}", subject: null, request);
        using var delete = await SendAsync(HttpMethod.Delete, $"/api/allergy-types/{allergyType.Id}", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, update.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, delete.StatusCode);
        Assert.Contains(allergyType, Factory.Store.AllergyTypes);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task AllergyTypeWrites_AreAdministratorOnly(string role)
    {
        var world = SeedWorld();
        var allergyType = AddAllergyType();
        var subject = world.SubjectFor(role);
        var request = new AllergyTypeRequest(Guid.Empty, "Latex");

        using var create = await SendAsync(HttpMethod.Post, "/api/allergy-types", subject, request);
        using var update = await SendAsync(HttpMethod.Put, $"/api/allergy-types/{allergyType.Id}", subject, request);
        using var delete = await SendAsync(HttpMethod.Delete, $"/api/allergy-types/{allergyType.Id}", subject);

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Contains(allergyType, Factory.Store.AllergyTypes);
    }

    [Fact]
    public async Task AllergyTypeWrites_AdministratorCanCreate()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Post,
            "/api/allergy-types",
            world.SubjectFor("admin"),
            new AllergyTypeRequest(Guid.Empty, "Penicillin Test"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(Factory.Store.AllergyTypes, allergyType => allergyType.Name == "Penicillin Test");
    }

    private AllergyType AddAllergyType()
    {
        var allergyType = AllergyType.Create(Guid.NewGuid(), "Peanuts");
        Factory.Store.AllergyTypes.Add(allergyType);
        return allergyType;
    }

    [Fact]
    public async Task AllergyTypePatients_RequireSignIn()
    {
        var allergyType = AddAllergyType();

        using var response = await SendAsync(HttpMethod.Get, $"/api/allergy-types/{allergyType.Id}/patients", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task AllergyTypePatients_AreAdministratorOnly(string role)
    {
        var world = SeedWorld();
        var allergyType = AddAllergyType();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/allergy-types/{allergyType.Id}/patients",
            world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AllergyTypePatients_AdministratorCanView()
    {
        var world = SeedWorld();
        var allergyType = AddAllergyType();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/allergy-types/{allergyType.Id}/patients",
            world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_RequiresSignIn()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/dashboard", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task Dashboard_IsAdministratorOnly(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/dashboard", world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_AdministratorCanView()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/dashboard", world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PatientSignUp_AllowsFreshSignedInUserWithoutPatientRole()
    {
        var subject = Subject("new");
        var request = new PatientRequest(Guid.Empty, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com");

        using var response = await SendAsync(HttpMethod.Post, "/api/me/patient", subject, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(Factory.Store.Patients, patient => patient.UserId == Factory.Store.UserFor(subject).Id);
    }

    [Fact]
    public async Task PatientSignUp_RejectsDuplicateProfileForSameIdentity()
    {
        var subject = Subject("new");
        var request = new PatientRequest(Guid.Empty, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com");

        using var first = await SendAsync(HttpMethod.Post, "/api/me/patient", subject, request);
        using var second = await SendAsync(HttpMethod.Post, "/api/me/patient", subject, request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Single(Factory.Store.Patients, patient => patient.UserId == Factory.Store.UserFor(subject).Id);
    }

    [Fact]
    public async Task PatientSignUp_RequiresSignIn()
    {
        var request = new PatientRequest(Guid.Empty, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com");

        using var response = await SendAsync(HttpMethod.Post, "/api/me/patient", subject: null, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MyDashboard_RequiresSignIn()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/me/dashboard", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MyDashboard_AllowsSignedInUserWithoutRolesAndReturnsNullSections()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/me/dashboard", Subject("fresh"));
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("admin").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("clinicAdmin").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("doctor").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("patient").ValueKind);
    }

    [Fact]
    public async Task MyDashboard_FillsOnlyTheSectionForAdministrator()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me/dashboard", world.SubjectFor("admin"));
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(root.GetProperty("admin").TryGetProperty("pendingProviderVerificationCount", out _));
        Assert.True(root.GetProperty("admin").TryGetProperty("todayAppointmentCount", out _));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("clinicAdmin").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, root.GetProperty("doctor").ValueKind);
    }

    [Fact]
    public async Task MyDashboard_ClinicAdminCountsOnlyTheirClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me/dashboard", world.ClinicAdminSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var section = document.RootElement.GetProperty("clinicAdmin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, section.GetProperty("clinicCount").GetInt32());
        Assert.Equal(1, section.GetProperty("providerCount").GetInt32());
        Assert.Equal(1, section.GetProperty("patientCount").GetInt32());
        Assert.Equal(1, section.GetProperty("pendingJoinRequestCount").GetInt32());
        Assert.Equal(0, section.GetProperty("todayAppointmentCount").GetInt32());
        Assert.True(JsonValueKindNull(document.RootElement, "admin"));
    }

    [Fact]
    public async Task MyDashboard_DoctorSectionHasCountsAndNextAppointment()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me/dashboard", world.DoctorSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var section = document.RootElement.GetProperty("doctor");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, section.GetProperty("todayAppointmentCount").GetInt32());
        Assert.Equal(0, section.GetProperty("weekAppointmentCount").GetInt32());
        Assert.Equal(1, section.GetProperty("distinctPatientCount").GetInt32());
        Assert.Equal(world.Appointment.Id, section.GetProperty("nextAppointment").GetProperty("id").GetGuid());
        Assert.True(JsonValueKindNull(document.RootElement, "patient"));
    }

    [Fact]
    public async Task MyDashboard_PatientSectionHasNextAppointmentAndUpcomingCount()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me/dashboard", world.PatientSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var section = document.RootElement.GetProperty("patient");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, section.GetProperty("upcomingAppointmentCount").GetInt32());
        Assert.Equal(world.Appointment.Id, section.GetProperty("nextAppointment").GetProperty("id").GetGuid());
        Assert.True(JsonValueKindNull(document.RootElement, "doctor"));
    }

    private static bool JsonValueKindNull(System.Text.Json.JsonElement root, string property) =>
        root.GetProperty(property).ValueKind == System.Text.Json.JsonValueKind.Null;

    [Fact]
    public async Task Me_RequiresSignIn()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/me", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_AllowsSignedInUserWithoutAnyRole()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/me", Subject("fresh"));
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(document.RootElement.GetProperty("clinicAdminOf").EnumerateArray());
    }

    [Theory]
    [InlineData("Ada Lovelace", "ada", "Ada Lovelace")]
    [InlineData(null, "ada", "ada")]
    [InlineData(null, null, null)]
    public async Task Me_DisplayNameComesFromNameClaimThenPreferredUsername(
        string? name,
        string? preferredUsername,
        string? expected)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            Factory.MintToken(Subject("named"), name, preferredUsername));
        using var response = await Factory.CreateClient().SendAsync(request);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var displayName = document.RootElement.GetProperty("displayName");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        if (expected is null)
            Assert.Equal(System.Text.Json.JsonValueKind.Null, displayName.ValueKind);
        else
            Assert.Equal(expected, displayName.GetString());
    }

    [Fact]
    public async Task Me_ListsOnlyTheClinicsTheCallerAdministers()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me", world.ClinicAdminSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var clinicAdminOf = document.RootElement.GetProperty("clinicAdminOf")
            .EnumerateArray()
            .Select(element => element.GetString())
            .ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([world.Clinic.Id.ToString()], clinicAdminOf);
    }

    [Fact]
    public async Task PatientRoster_RequiresSignIn()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/patients", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("doctor")]
    [InlineData("admin")]
    [InlineData("clinicadmin")]
    public async Task PatientRoster_AllowsProviderAdministratorAndClinicAdminOfTheClinic(string role)
    {
        var world = SeedWorld();
        using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/patients", world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("other-doctor")]
    [InlineData("other-clinicadmin")]
    public async Task PatientRoster_ForbidsEveryoneElse(string who)
    {
        var world = SeedWorld();
        var subject = who switch
        {
            "patient" => world.PatientSubject,
            "other-doctor" => world.OtherDoctorSubject,
            _ => world.OtherClinicAdminSubject
        };

        using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/patients", subject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatientRoster_ReturnsPatientsWithVisitsAndCount()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/patients", world.DoctorSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var row = document.RootElement.GetProperty("items").EnumerateArray().Single();
        var patient = row.GetProperty("patient");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(world.Patient.Id, patient.GetProperty("id").GetGuid());
        Assert.Equal("Ada", patient.GetProperty("firstName").GetString());
        Assert.Equal("Lovelace", patient.GetProperty("lastName").GetString());
        Assert.Equal("1985-12-10", patient.GetProperty("dateOfBirth").GetString());
        Assert.Equal("ada@example.com", patient.GetProperty("email").GetString());
        Assert.Equal("555-0102", patient.GetProperty("phoneNumber").GetString());
        Assert.Equal(1, row.GetProperty("appointmentCount").GetInt32());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, row.GetProperty("lastVisit").ValueKind);
        Assert.Equal(world.Appointment.Id, row.GetProperty("nextVisit").GetProperty("id").GetGuid());
        Assert.Equal("Checkup", row.GetProperty("nextVisit").GetProperty("reasonForVisit").GetString());
    }

    [Fact]
    public async Task PatientRoster_SearchMatchesPatientName()
    {
        var world = SeedWorld();

        using var matching = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/patients?search=lovel", world.DoctorSubject);
        using var other = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/patients?search=hopper", world.DoctorSubject);
        using var matchingDocument = System.Text.Json.JsonDocument.Parse(await matching.Content.ReadAsStringAsync());
        using var otherDocument = System.Text.Json.JsonDocument.Parse(await other.Content.ReadAsStringAsync());

        Assert.Equal(1, matchingDocument.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, otherDocument.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task AppointmentStatus_IsTheEnumNameOnDetailAndProviderListings()
    {
        var world = SeedWorld();

        using var detail = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}", world.PatientSubject);
        using var listing = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}/appointments", world.DoctorSubject);
        using var detailDocument = System.Text.Json.JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        using var listingDocument = System.Text.Json.JsonDocument.Parse(await listing.Content.ReadAsStringAsync());
        var listed = listingDocument.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == world.Appointment.Id);

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal("REQUESTED", detailDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal("REQUESTED", listed.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AppointmentStatus_ConfirmThenCompleteFollowsTheRoutes()
    {
        var world = SeedWorld();
        var id = world.Appointment.Id;

        using var confirm = await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/confirm", world.DoctorSubject);
        var afterConfirm = await ReadStatusAsync(id, world.PatientSubject);
        using var complete = await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/complete", world.DoctorSubject);
        var afterComplete = await ReadStatusAsync(id, world.PatientSubject);

        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        Assert.Equal("CONFIRMED", afterConfirm);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);
        Assert.Equal("COMPLETED", afterComplete);
    }

    [Fact]
    public async Task AppointmentStatus_DeclineRouteMovesRequestedToDeclined()
    {
        var world = SeedWorld();

        using var decline = await SendAsync(HttpMethod.Post, $"/api/appointments/{world.Appointment.Id}/decline", world.DoctorSubject);
        var status = await ReadStatusAsync(world.Appointment.Id, world.PatientSubject);

        Assert.Equal(HttpStatusCode.NoContent, decline.StatusCode);
        Assert.Equal("DECLINED", status);
    }

    [Fact]
    public async Task AppointmentStatus_PatientCancelRouteMovesRequestedToCancelled()
    {
        var world = SeedWorld();
        var appointment = Appointment.Create(
            DateTimeOffset.UtcNow.AddDays(32),
            "Cancel me",
            world.Patient,
            world.Doctor,
            DateTimeOffset.UtcNow);
        Factory.Store.Appointments.Add(appointment);

        using var cancel = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointment.Id}/cancel", world.PatientSubject);
        var status = await ReadStatusAsync(appointment.Id, world.PatientSubject);

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal("CANCELLED", status);
    }

    [Fact]
    public async Task AppointmentStatus_CompleteOnRequestedAppointmentIsConflict()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Post, $"/api/appointments/{world.Appointment.Id}/complete", world.DoctorSubject);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("REQUESTED", await ReadStatusAsync(world.Appointment.Id, world.PatientSubject));
    }

    private async Task<string?> ReadStatusAsync(Guid appointmentId, string subject)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/api/appointments/{appointmentId}", subject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("status").GetString();
    }

    [Fact]
    public async Task CalendarSearch_RequiresSignIn()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/appointments/search?pageSize=100", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CalendarSearch_ForbidsUsersWithoutPatientProviderOrClinicRole()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/appointments/search?pageSize=100", Subject("fresh"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CalendarSearch_FiltersByRepeatedProviderIds()
    {
        var world = SeedWorld();
        var otherAppointment = Appointment.Create(
            DateTimeOffset.UtcNow.AddDays(31),
            "Other visit",
            world.OtherPatient,
            world.OtherDoctor,
            DateTimeOffset.UtcNow);
        Factory.Store.Appointments.Add(otherAppointment);

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/appointments/search?pageSize=100&providerIds={world.Doctor.Id}&providerIds={Guid.NewGuid()}",
            world.SubjectFor("admin"));
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var ids = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(world.Appointment.Id, ids);
        Assert.DoesNotContain(otherAppointment.Id, ids);
    }

    [Fact]
    public async Task CalendarSearch_ItemsCarryPatientProviderSpecialtyAndStatus()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/appointments/search?pageSize=100&providerIds={world.Doctor.Id}",
            world.SubjectFor("admin"));
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(element => element.GetProperty("id").GetGuid() == world.Appointment.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Ada Lovelace", item.GetProperty("patientName").GetString());
        Assert.Equal("Alan Turing", item.GetProperty("providerName").GetString());
        Assert.Equal("Computing", item.GetProperty("providerSpecialty").GetString());
        Assert.Equal("REQUESTED", item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CalendarSearch_RejectsFromThatIsNotBeforeTo()
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            "/api/appointments/search?pageSize=100&from=2030-01-01T00:00:00Z&to=2030-01-01T00:00:00Z",
            ClinicApiFactory.PlatformAdminSubject);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CalendarSearch_DoctorMayOnlyRequestOwnProviderId()
    {
        var world = SeedWorld();

        using var own = await SendAsync(
            HttpMethod.Get,
            $"/api/appointments/search?pageSize=100&providerIds={world.Doctor.Id}",
            world.DoctorSubject);
        using var other = await SendAsync(
            HttpMethod.Get,
            $"/api/appointments/search?pageSize=100&providerIds={world.OtherDoctor.Id}",
            world.DoctorSubject);

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
    }

    [Fact]
    public async Task CalendarSearch_PatientSeesOnlyOwnAppointmentsWhateverProviderIdsAreAsked()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/appointments/search?pageSize=100&providerIds={world.Doctor.Id}",
            world.PatientSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var patientIds = document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("patientId").GetGuid())
            .Distinct()
            .ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([world.Patient.Id], patientIds);
    }

    [Fact]
    public async Task Me_ClinicAdminOfIsEmptyForDoctors()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me", world.DoctorSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Empty(document.RootElement.GetProperty("clinicAdminOf").EnumerateArray());
    }

    [Fact]
    public async Task BusySlots_IsPublicToAnonymousCallers()
    {
        var world = SeedWorld();
        var day = BusyDay();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{world.Doctor.Id}/busy-slots?date={day:yyyy-MM-dd}",
            subject: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BusySlots_ReturnOnlyActiveStartTimesForTheDayWithoutPatientData()
    {
        var world = SeedWorld();
        var day = BusyDay();
        var doctor = Provider.Create(Guid.NewGuid(), world.Clinic.Id, "Busy", "Doctor", "Cardiology");
        Factory.Store.Providers.Add(doctor);
        var dayStart = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var requested = AddAppointment(world.Patient, doctor, dayStart.AddHours(8), AppointmentStatus.REQUESTED);
        var confirmed = AddAppointment(world.Patient, doctor, dayStart.AddHours(8).AddMinutes(20), AppointmentStatus.CONFIRMED);
        var declined = AddAppointment(world.Patient, doctor, dayStart.AddHours(9), AppointmentStatus.DECLINED);
        var cancelled = AddAppointment(world.Patient, doctor, dayStart.AddHours(9).AddMinutes(20), AppointmentStatus.CANCELLED);
        AddAppointment(world.Patient, doctor, dayStart.AddMinutes(-20), AppointmentStatus.REQUESTED);
        AddAppointment(world.Patient, doctor, dayStart.AddDays(1), AppointmentStatus.REQUESTED);

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{doctor.Id}/busy-slots?date={day:yyyy-MM-dd}",
            subject: null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = System.Text.Json.JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ["providerId", "date", "appointmentDurationMinutes", "busyStartTimes"],
            root.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(doctor.Id, root.GetProperty("providerId").GetGuid());
        Assert.Equal(day.ToString("yyyy-MM-dd"), root.GetProperty("date").GetString());
        Assert.Equal(20, root.GetProperty("appointmentDurationMinutes").GetInt32());
        var busy = root.GetProperty("busyStartTimes").EnumerateArray()
            .Select(element => DateTimeOffset.Parse(element.GetString()!))
            .ToList();
        Assert.Equal([requested.AppointmentDateTime, confirmed.AppointmentDateTime], busy);
        Assert.DoesNotContain(declined.AppointmentDateTime.ToString("O"), body);
        Assert.DoesNotContain(cancelled.AppointmentDateTime.ToString("O"), body);
        Assert.DoesNotContain("Ada", body);
        Assert.DoesNotContain("Lovelace", body);
        Assert.DoesNotContain(world.Patient.Id.ToString(), body);
        Assert.DoesNotContain(requested.Id.ToString(), body);
        Assert.DoesNotContain("Checkup", body);
    }

    [Fact]
    public async Task BusySlots_UnknownProviderIsNotFound()
    {
        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{Guid.NewGuid()}/busy-slots?date={BusyDay():yyyy-MM-dd}",
            subject: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BusySlots_UnverifiedAndUnassignedProvidersAreNotFound()
    {
        var world = SeedWorld();
        var unassigned = Provider.Register(Guid.NewGuid(), "Unassigned", "Doctor", "General");
        unassigned.Verify();
        Factory.Store.Providers.Add(unassigned);
        var day = BusyDay().ToString("yyyy-MM-dd");

        using var pending = await SendAsync(HttpMethod.Get, $"/api/providers/{world.PendingProvider.Id}/busy-slots?date={day}", subject: null);
        using var noClinic = await SendAsync(HttpMethod.Get, $"/api/providers/{unassigned.Id}/busy-slots?date={day}", subject: null);

        Assert.Equal(HttpStatusCode.NotFound, pending.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, noClinic.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?date=")]
    [InlineData("?date=2030-02-30")]
    [InlineData("?date=2030-1-5")]
    [InlineData("?date=tomorrow")]
    public async Task BusySlots_RejectMissingOrInvalidDate(string query)
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{world.Doctor.Id}/busy-slots{query}",
            subject: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ClinicList_CountsOnlyVerifiedProvidersOfEachClinic()
    {
        var world = SeedWorld();
        var clinic = AddClinic($"Count {Guid.NewGuid():N}");
        AddVerifiedProvider(clinic, "Ada", "Cole", "Cardiology");
        AddVerifiedProvider(clinic, "Ben", "Dunn", "Neurology");
        var unassigned = Provider.Register(Guid.NewGuid(), "Pending", "Person", "Cardiology");
        Factory.Store.Providers.Add(unassigned);

        using var response = await SendAsync(HttpMethod.Get, "/api/clinics", world.PatientSubject);
        using var document = await ReadJsonAsync(response);
        var item = document.RootElement.EnumerateArray().Single(element => element.GetProperty("id").GetGuid() == clinic.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, item.GetProperty("providerCount").GetInt32());
        Assert.Equal(clinic.Name, item.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ClinicSearch_IncludesProviderCountOnEachItem()
    {
        var world = SeedWorld();
        var name = $"Searchable {Guid.NewGuid():N}";
        var clinic = AddClinic(name);
        AddVerifiedProvider(clinic, "Ada", "Cole", "Cardiology");

        using var response = await SendAsync(HttpMethod.Get, $"/api/clinics/search?search={Uri.EscapeDataString(name)}", world.PatientSubject);
        using var document = await ReadJsonAsync(response);
        var item = document.RootElement.GetProperty("items").EnumerateArray().Single();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, item.GetProperty("providerCount").GetInt32());
        Assert.Equal(clinic.Id, item.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task ClinicSummary_IsAvailableToAnySignedInRole()
    {
        var world = SeedWorld();
        var clinic = AddClinic($"Open {Guid.NewGuid():N}");

        foreach (var role in new[] { "patient", "doctor", "clinicadmin", "admin" })
        {
            using var response = await SendAsync(HttpMethod.Get, $"/api/clinics/{clinic.Id}/summary", world.SubjectFor(role));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ClinicSummary_UnknownClinicIsNotFound()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/clinics/{Guid.NewGuid()}/summary", world.DoctorSubject);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ClinicSummary_ReturnsCountsSpecialtiesAndProvidersInCamelCase()
    {
        var world = SeedWorld();
        var clinic = AddClinic($"Summary {Guid.NewGuid():N}");
        var ada = AddVerifiedProvider(clinic, "Ada", "Cole", "Cardiology");
        var ben = AddVerifiedProvider(clinic, "Ben", "Cole", "Cardiology");
        AddVerifiedProvider(clinic, "Cy", "Dunn", "Neurology");
        Factory.Store.Providers.Add(Provider.Register(Guid.NewGuid(), "Pending", "Person", "Cardiology"));
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        AddAppointment(world.Patient, ada, todayStart.AddHours(9), AppointmentStatus.REQUESTED);
        AddAppointment(world.Patient, ada, todayStart.AddHours(10), AppointmentStatus.DECLINED);
        AddAppointment(world.Patient, ben, todayStart.AddHours(11), AppointmentStatus.CANCELLED);
        AddAppointment(world.Patient, ben, todayStart.AddHours(12), AppointmentStatus.CONFIRMED);
        AddAppointment(world.Patient, ben, todayStart.AddDays(1).AddHours(9), AppointmentStatus.CONFIRMED);

        using var response = await SendAsync(HttpMethod.Get, $"/api/clinics/{clinic.Id}/summary", world.DoctorSubject);
        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ["clinic", "providerCount", "todayAppointmentCount", "pendingJoinRequestCount", "topSpecialties", "providers"],
            root.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(
            ["id", "name", "address", "phoneNumber"],
            root.GetProperty("clinic").EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(clinic.Id, root.GetProperty("clinic").GetProperty("id").GetGuid());
        Assert.Equal(3, root.GetProperty("providerCount").GetInt32());
        Assert.Equal(2, root.GetProperty("todayAppointmentCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("pendingJoinRequestCount").ValueKind);
        Assert.Equal(
            ["Cardiology", "Neurology"],
            root.GetProperty("topSpecialties").EnumerateArray().Select(element => element.GetString()!).ToArray());
        var providers = root.GetProperty("providers").EnumerateArray().ToList();
        Assert.Equal(
            ["Ada Cole", "Ben Cole", "Cy Dunn"],
            providers.Select(provider => $"{provider.GetProperty("firstName").GetString()} {provider.GetProperty("lastName").GetString()}").ToArray());
        Assert.Equal(clinic.Name, providers[0].GetProperty("clinicName").GetString());
    }

    [Fact]
    public async Task ClinicSummary_ShowsPendingJoinRequestsOnlyToAdministratorsOfThisClinic()
    {
        var world = SeedWorld();
        var clinic = AddClinic($"Requests {Guid.NewGuid():N}");
        var ownAdminSubject = Subject("clinicadmin");
        Factory.Store.ClinicAdmins.Add(ClinicAdmin.Create(Guid.NewGuid(), clinic.Id, Factory.Store.UserFor(ownAdminSubject).Id));
        Factory.Store.JoinRequests.Add(ClinicJoinRequest.Create(Guid.NewGuid(), world.PendingProvider.Id, clinic.Id));

        Assert.Equal(1, await PendingJoinRequestCountAsync(clinic.Id, world.SubjectFor("admin")));
        Assert.Equal(1, await PendingJoinRequestCountAsync(clinic.Id, ownAdminSubject));
        Assert.Null(await PendingJoinRequestCountAsync(clinic.Id, world.ClinicAdminSubject));
        Assert.Null(await PendingJoinRequestCountAsync(clinic.Id, world.DoctorSubject));
        Assert.Null(await PendingJoinRequestCountAsync(clinic.Id, world.PatientSubject));
    }

    [Fact]
    public async Task ProviderSummary_IsPublicAndShowsTodaysActiveCountWithoutPatientData()
    {
        var world = SeedWorld();
        var doctor = AddVerifiedProvider(world.Clinic, "Summary", "Doctor", "Cardiology");
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        AddAppointment(world.Patient, doctor, todayStart.AddHours(9), AppointmentStatus.REQUESTED);
        AddAppointment(world.Patient, doctor, todayStart.AddHours(10), AppointmentStatus.DECLINED);
        AddAppointment(world.Patient, doctor, todayStart.AddHours(11), AppointmentStatus.CANCELLED);
        AddAppointment(world.Patient, doctor, todayStart.AddDays(1).AddHours(9), AppointmentStatus.CONFIRMED);

        using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{doctor.Id}/summary", subject: null);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ["provider", "clinic", "nextAvailableStart", "todayAppointmentCount"],
            root.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(doctor.Id, root.GetProperty("provider").GetProperty("id").GetGuid());
        Assert.Equal(world.Clinic.Name, root.GetProperty("provider").GetProperty("clinicName").GetString());
        Assert.Equal(world.Clinic.Name, root.GetProperty("clinic").GetProperty("name").GetString());
        Assert.Equal(1, root.GetProperty("todayAppointmentCount").GetInt32());

        // Slots start on the 20-minute grid from 08:00 and the last one begins at 15:40 (ends at 16:00).
        var next = root.GetProperty("nextAvailableStart");
        Assert.Equal(JsonValueKind.String, next.ValueKind);
        var start = DateTimeOffset.Parse(next.GetString()!);
        Assert.InRange(start.TimeOfDay, TimeSpan.FromHours(8), TimeSpan.FromMinutes(940));
        Assert.Equal(0, (int)(start.TimeOfDay - TimeSpan.FromHours(8)).TotalMinutes % 20);

        Assert.DoesNotContain("Checkup", body);
        Assert.DoesNotContain("Lovelace", body);
        Assert.DoesNotContain(world.Patient.Id.ToString(), body);
    }

    [Fact]
    public async Task ProviderSummary_UnknownUnverifiedAndUnassignedProvidersAreNotFound()
    {
        var world = SeedWorld();
        var unassigned = Provider.Register(Guid.NewGuid(), "Unassigned", "Doctor", "General");
        unassigned.Verify();
        Factory.Store.Providers.Add(unassigned);

        foreach (var id in new[] { Guid.NewGuid(), world.PendingProvider.Id, unassigned.Id })
        {
            using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{id}/summary", subject: null);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    private Clinic AddClinic(string name)
    {
        var clinic = Clinic.Create(Guid.NewGuid(), name, "9 Test Street", "555-0199");
        Factory.Store.Clinics.Add(clinic);
        return clinic;
    }

    private Provider AddVerifiedProvider(Clinic clinic, string firstName, string lastName, string specialty)
    {
        var provider = Provider.Create(Guid.NewGuid(), clinic.Id, firstName, lastName, specialty);
        Factory.Store.Providers.Add(provider);
        return provider;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private async Task<int?> PendingJoinRequestCountAsync(Guid clinicId, string subject)
    {
        using var response = await SendAsync(HttpMethod.Get, $"/api/clinics/{clinicId}/summary", subject);
        using var document = await ReadJsonAsync(response);
        var value = document.RootElement.GetProperty("pendingJoinRequestCount");
        return value.ValueKind == JsonValueKind.Null ? null : value.GetInt32();
    }

    private static DateOnly BusyDay() => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

    private Appointment AddAppointment(Patient patient, Provider provider, DateTimeOffset when, AppointmentStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = Appointment.Create(when, "Checkup", patient, provider, now);
        switch (status)
        {
            case AppointmentStatus.CONFIRMED:
                appointment.Confirm(now);
                break;
            case AppointmentStatus.DECLINED:
                appointment.Decline(now);
                break;
            case AppointmentStatus.CANCELLED:
                appointment.Cancel(now);
                break;
        }
        Factory.Store.Appointments.Add(appointment);
        return appointment;
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(15, 40)]
    public async Task Booking_AcceptsStartAtClinicOpeningAndEndAtClosing(int hour, int minute)
    {
        var world = SeedWorld();
        var when = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(4).AddHours(hour).AddMinutes(minute), TimeSpan.Zero);
        var request = new AppointmentRequest(Guid.NewGuid(), world.Patient.Id, world.Doctor.Id, when, "Checkup");

        using var response = await SendAsync(HttpMethod.Post, "/api/appointments", world.PatientSubject, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Booking_RejectsStartOneMinuteBeforeClinicOpening()
    {
        var world = SeedWorld();
        var when = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(4).AddHours(7).AddMinutes(59), TimeSpan.Zero);
        var request = new AppointmentRequest(Guid.NewGuid(), world.Patient.Id, world.Doctor.Id, when, "Checkup");

        using var response = await SendAsync(HttpMethod.Post, "/api/appointments", world.PatientSubject, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(Factory.Store.Appointments, appointment => appointment.Id == request.Id);
    }

    [Fact]
    public async Task ProviderLookup_IsPublicAndCarriesClinicName()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{world.Doctor.Id}", subject: null);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Riverside Clinic", document.RootElement.GetProperty("clinicName").GetString());
    }

    [Fact]
    public async Task ProviderLookup_ClinicNameIsNullForProviderWithoutClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/providers/{world.PendingProvider.Id}", subject: null);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, document.RootElement.GetProperty("clinicName").ValueKind);
    }

    [Fact]
    public async Task ClinicProviders_RequireSignInAndCarryClinicName()
    {
        var world = SeedWorld();

        using var anonymous = await SendAsync(HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/providers", subject: null);
        using var signedIn = await SendAsync(HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/providers", world.DoctorSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await signedIn.Content.ReadAsStringAsync());
        var doctor = document.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == world.Doctor.Id);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        Assert.Equal("Riverside Clinic", doctor.GetProperty("clinicName").GetString());
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task AllProviders_IsAdministratorOnly(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/providers", world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AllProviders_AdministratorSeesClinicName()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/providers", world.SubjectFor("admin"));
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var doctor = document.RootElement.EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == world.Doctor.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Riverside Clinic", doctor.GetProperty("clinicName").GetString());
    }

    [Fact]
    public async Task ProviderDirectory_CarriesClinicNameForListedProviders()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/providers/directory", subject: null);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var doctor = document.RootElement.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == world.Doctor.Id);

        Assert.Equal("Riverside Clinic", doctor.GetProperty("clinicName").GetString());
    }

    [Fact]
    public async Task AppointmentProvider_RequiresSignIn()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}/provider", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AppointmentProvider_ForbidsUnrelatedPatient()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}/provider", world.OtherPatientSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AppointmentProvider_AllowsParticipantAndCarriesClinicName()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/appointments/{world.Appointment.Id}/provider", world.PatientSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(world.Doctor.Id, document.RootElement.GetProperty("id").GetGuid());
        Assert.Equal("Riverside Clinic", document.RootElement.GetProperty("clinicName").GetString());
    }
    [Fact]
    public async Task JoinRequestDecisions_AllowPlatformAdministrator()
    {
        var world = SeedWorld();
        var subject = world.SubjectFor("admin");
        var toApprove = AddPendingJoinRequest(world);
        var toReject = AddPendingJoinRequest(world);

        using var approve = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{toApprove.Id}/approve", subject);
        using var reject = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{toReject.Id}/reject", subject);

        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, reject.StatusCode);
        Assert.Equal(ClinicJoinRequestStatus.APPROVED, toApprove.Status);
        Assert.Equal(ClinicJoinRequestStatus.REJECTED, toReject.Status);
    }

    [Fact]
    public async Task JoinRequestList_AllowsPlatformAdministratorAndClinicAdminOfThatClinic()
    {
        var world = SeedWorld();

        using var admin = await SendAsync(HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/join-requests", world.SubjectFor("admin"));
        using var clinicAdmin = await SendAsync(HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/join-requests", world.ClinicAdminSubject);
        using var document = System.Text.Json.JsonDocument.Parse(await admin.Content.ReadAsStringAsync());
        var ids = document.RootElement.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();

        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, clinicAdmin.StatusCode);
        Assert.Contains(world.JoinRequest.Id, ids);
    }

    [Fact]
    public async Task JoinRequestList_ForbidsClinicAdminOfAnotherClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/join-requests", world.OtherClinicAdminSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    public async Task JoinRequestList_ForbidsPatientsAndDoctors(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/join-requests", world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private ClinicJoinRequest AddPendingJoinRequest(World world)
    {
        var request = ClinicJoinRequest.Create(Guid.NewGuid(), Guid.NewGuid(), world.Clinic.Id);
        Factory.Store.JoinRequests.Add(request);
        return request;
    }
}
