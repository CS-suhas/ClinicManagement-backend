using System.Net;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests;

/// <summary>GET /api/patients/{id}/summary: who may read it, and what each caller sees.</summary>
public sealed class PatientSummaryTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    [Fact]
    public async Task Summary_RequiresSignIn()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsAllowedForAdministrator()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsAllowedForThePatientThemself()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.PatientSubject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsForbiddenForAnotherPatient()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.OtherPatientSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsAllowedForDoctorWithAnAppointmentWithThePatient()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.DoctorSubject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsForbiddenForDoctorWithoutAnAppointmentWithThePatient()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.OtherDoctorSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsForbiddenForUnverifiedProviderEvenWithAnAppointment()
    {
        var world = SeedWorld();
        AddAppointment(world.Patient, world.PendingProvider, Days(4), AppointmentStatus.REQUESTED);

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.PendingSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsAllowedForClinicAdminOfAClinicWhereThePatientHasAnAppointment()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.ClinicAdminSubject);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Summary_IsForbiddenForClinicAdminOfAnotherClinic()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, SummaryUrl(world.Patient), world.OtherClinicAdminSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_UnknownPatientIsNotFoundForAdministrator()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/patients/{Guid.NewGuid()}/summary",
            world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Summary_UnknownPatientIsForbiddenForOtherCallers()
    {
        var world = SeedWorld();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/patients/{Guid.NewGuid()}/summary",
            world.DoctorSubject);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_ReturnsPatientAndAllergiesInCamelCase()
    {
        var world = SeedWorld();
        world.Patient.AddAllergy(AllergyType.Create("Penicillin"), AllergySeverity.SEVERE, null);

        var json = await GetJsonAsync(SummaryUrl(world.Patient), world.PatientSubject);

        var patient = json.GetProperty("patient");
        Assert.Equal(world.Patient.Id, patient.GetProperty("id").GetGuid());
        Assert.Equal("Ada", patient.GetProperty("firstName").GetString());
        Assert.Equal("Lovelace", patient.GetProperty("lastName").GetString());
        Assert.Equal("1985-12-10", patient.GetProperty("dateOfBirth").GetString());
        Assert.Equal("ada@example.com", patient.GetProperty("email").GetString());
        Assert.Equal("555-0102", patient.GetProperty("phoneNumber").GetString());

        var allergy = Assert.Single(json.GetProperty("allergies").EnumerateArray());
        Assert.Equal("Penicillin", allergy.GetProperty("name").GetString());
        Assert.Equal("SEVERE", allergy.GetProperty("severity").GetString());
        Assert.Equal(world.Patient.Allergies.Single().AllergyId, allergy.GetProperty("allergyId").GetGuid());
    }

    [Fact]
    public async Task Summary_ForAdministratorCountsEveryAppointmentAndSkipsDeclinedAndCancelledForVisits()
    {
        var world = SeedWorld();
        var patient = world.Patient;
        var nextAdminVisit = AddAppointment(patient, world.OtherDoctor, Days(2), AppointmentStatus.CONFIRMED);
        AddAppointment(patient, world.Doctor, Days(5), AppointmentStatus.CONFIRMED);
        var lastVisit = AddAppointment(patient, world.Doctor, Days(-10), AppointmentStatus.COMPLETED);
        AddAppointment(patient, world.Doctor, Days(3), AppointmentStatus.DECLINED);
        AddAppointment(patient, world.Doctor, Days(1), AppointmentStatus.CANCELLED);

        var json = await GetJsonAsync(SummaryUrl(patient), world.SubjectFor("admin"));

        var counts = json.GetProperty("counts");
        Assert.Equal(6, counts.GetProperty("total").GetInt32());
        Assert.Equal(1, counts.GetProperty("requested").GetInt32());
        Assert.Equal(2, counts.GetProperty("confirmed").GetInt32());
        Assert.Equal(1, counts.GetProperty("completed").GetInt32());
        Assert.Equal(1, counts.GetProperty("cancelled").GetInt32());
        Assert.Equal(1, counts.GetProperty("declined").GetInt32());

        Assert.Equal(nextAdminVisit.Id, json.GetProperty("nextVisit").GetProperty("id").GetGuid());
        Assert.Equal(lastVisit.Id, json.GetProperty("lastVisit").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Summary_VisitCarriesProviderNameStatusAndReason()
    {
        var world = SeedWorld();

        var json = await GetJsonAsync(SummaryUrl(world.Patient), world.PatientSubject);

        var nextVisit = json.GetProperty("nextVisit");
        Assert.Equal(world.Appointment.Id, nextVisit.GetProperty("id").GetGuid());
        Assert.Equal("Alan Turing", nextVisit.GetProperty("providerName").GetString());
        Assert.Equal("REQUESTED", nextVisit.GetProperty("status").GetString());
        Assert.Equal("Checkup", nextVisit.GetProperty("reasonForVisit").GetString());
        Assert.Equal(
            world.Appointment.AppointmentDateTime,
            nextVisit.GetProperty("appointmentDateTime").GetDateTimeOffset());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("lastVisit").ValueKind);
    }

    [Fact]
    public async Task Summary_DoctorSeesOnlyTheirOwnAppointmentsInVisitsAndCounts()
    {
        var world = SeedWorld();
        var patient = world.Patient;
        AddAppointment(patient, world.OtherDoctor, Days(2), AppointmentStatus.CONFIRMED);
        var ownNext = AddAppointment(patient, world.Doctor, Days(5), AppointmentStatus.CONFIRMED);
        var ownLast = AddAppointment(patient, world.Doctor, Days(-10), AppointmentStatus.COMPLETED);
        AddAppointment(patient, world.Doctor, Days(3), AppointmentStatus.DECLINED);
        AddAppointment(patient, world.Doctor, Days(1), AppointmentStatus.CANCELLED);

        var json = await GetJsonAsync(SummaryUrl(patient), world.DoctorSubject);

        var counts = json.GetProperty("counts");
        Assert.Equal(5, counts.GetProperty("total").GetInt32());
        Assert.Equal(1, counts.GetProperty("requested").GetInt32());
        Assert.Equal(1, counts.GetProperty("confirmed").GetInt32());
        Assert.Equal(1, counts.GetProperty("completed").GetInt32());
        Assert.Equal(1, counts.GetProperty("cancelled").GetInt32());
        Assert.Equal(1, counts.GetProperty("declined").GetInt32());
        Assert.Equal(ownNext.Id, json.GetProperty("nextVisit").GetProperty("id").GetGuid());
        Assert.Equal(ownLast.Id, json.GetProperty("lastVisit").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Summary_DoctorNextVisitIgnoresOtherDoctorsEarlierAppointment()
    {
        var world = SeedWorld();
        AddAppointment(world.Patient, world.OtherDoctor, Days(2), AppointmentStatus.CONFIRMED);

        var json = await GetJsonAsync(SummaryUrl(world.Patient), world.DoctorSubject);

        Assert.Equal(world.Appointment.Id, json.GetProperty("nextVisit").GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("lastVisit").ValueKind);
    }

    [Fact]
    public async Task Summary_ClinicAdminSeesOnlyAppointmentsAtTheirClinics()
    {
        var world = SeedWorld();
        var patient = world.Patient;
        AddAppointment(patient, world.OtherDoctor, Days(2), AppointmentStatus.CONFIRMED);
        AddAppointment(patient, world.Doctor, Days(5), AppointmentStatus.CONFIRMED);
        AddAppointment(patient, world.Doctor, Days(-10), AppointmentStatus.COMPLETED);

        var json = await GetJsonAsync(SummaryUrl(patient), world.ClinicAdminSubject);

        var counts = json.GetProperty("counts");
        Assert.Equal(3, counts.GetProperty("total").GetInt32());
        Assert.Equal(1, counts.GetProperty("requested").GetInt32());
        Assert.Equal(1, counts.GetProperty("confirmed").GetInt32());
        Assert.Equal(1, counts.GetProperty("completed").GetInt32());
        Assert.Equal(3, json.GetProperty("recent").GetArrayLength());
    }

    [Fact]
    public async Task Summary_RecentHoldsThreeNewestVisitsFirst()
    {
        var world = SeedWorld();
        var patient = world.Patient;
        AddAppointment(patient, world.Doctor, Days(5), AppointmentStatus.CONFIRMED);
        AddAppointment(patient, world.Doctor, Days(-10), AppointmentStatus.COMPLETED);
        AddAppointment(patient, world.Doctor, Days(-20), AppointmentStatus.COMPLETED);

        var json = await GetJsonAsync(SummaryUrl(patient), world.SubjectFor("admin"));

        var times = json.GetProperty("recent").EnumerateArray()
            .Select(item => item.GetProperty("appointmentDateTime").GetDateTimeOffset())
            .ToList();
        Assert.Equal(3, times.Count);
        Assert.Equal(times.OrderByDescending(time => time).ToList(), times);
        Assert.Equal(world.Appointment.AppointmentDateTime, times[0]);
    }

    [Fact]
    public async Task Summary_PatientWithoutAppointmentsHasNullVisitsAndZeroCounts()
    {
        var world = SeedWorld();
        var lonely = Patient.Create(
            Guid.NewGuid(), "Edith", "Clarke", new DateOnly(1883, 1, 10), "edith@example.com", "555-0104");
        Factory.Store.Patients.Add(lonely);

        var json = await GetJsonAsync(SummaryUrl(lonely), world.SubjectFor("admin"));

        Assert.Equal(JsonValueKind.Null, json.GetProperty("lastVisit").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nextVisit").ValueKind);
        Assert.Equal(0, json.GetProperty("counts").GetProperty("total").GetInt32());
        Assert.Empty(json.GetProperty("recent").EnumerateArray());
        Assert.Empty(json.GetProperty("allergies").EnumerateArray());
    }

    private static string SummaryUrl(Patient patient) => $"/api/patients/{patient.Id}/summary";

    private static DateTimeOffset Days(int days) => DateTimeOffset.UtcNow.AddDays(days);

    private Appointment AddAppointment(Patient patient, Provider provider, DateTimeOffset when, AppointmentStatus status)
    {
        // Appointment.Create requires a future time, so the creation clock is set one day before the appointment.
        var appointment = Appointment.Create(Guid.NewGuid(), when, "Visit", patient, provider, when.AddDays(-1));
        switch (status)
        {
            case AppointmentStatus.CONFIRMED:
                appointment.Confirm(when);
                break;
            case AppointmentStatus.COMPLETED:
                appointment.Confirm(when);
                appointment.Complete(when);
                break;
            case AppointmentStatus.DECLINED:
                appointment.Decline(when);
                break;
            case AppointmentStatus.CANCELLED:
                appointment.Cancel(when);
                break;
        }

        Factory.Store.Appointments.Add(appointment);
        return appointment;
    }

    private async Task<JsonElement> GetJsonAsync(string url, string subject)
    {
        using var response = await SendAsync(HttpMethod.Get, url, subject);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
