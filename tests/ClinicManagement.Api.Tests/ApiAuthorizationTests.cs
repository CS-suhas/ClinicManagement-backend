using System.Net;
using System.Net.Http.Json;
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
    public async Task JoinRequestDecisions_ForbidPlatformAdministrator()
    {
        var world = SeedWorld();
        var subject = world.SubjectFor("admin");

        using var approve = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/approve", subject);
        using var reject = await SendAsync(HttpMethod.Post, $"/api/clinic-join-requests/{world.JoinRequest.Id}/reject", subject);
        using var list = await SendAsync(HttpMethod.Get, $"/api/clinics/{world.Clinic.Id}/join-requests", subject);

        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
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
        Assert.Single(Factory.Store.Patients, patient => patient.IdentitySubject == subject);
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
        Assert.Single(Factory.Store.Patients, patient => patient.IdentitySubject == subject);
    }

    [Fact]
    public async Task PatientSignUp_RequiresSignIn()
    {
        var request = new PatientRequest(Guid.Empty, "Ada", "Lovelace", new DateOnly(1985, 12, 10), "555-0100", "ada@example.com");

        using var response = await SendAsync(HttpMethod.Post, "/api/me/patient", subject: null, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

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
}

