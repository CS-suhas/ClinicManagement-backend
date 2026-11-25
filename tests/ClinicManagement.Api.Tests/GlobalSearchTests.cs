using System.Net;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Api.Tests;

/// <summary>GET /api/search: sign-in, validation, per-role visibility, ordering and limits.</summary>
public sealed class GlobalSearchTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    [Fact]
    public async Task Search_RequiresSignIn()
    {
        var token = NewToken();

        using var response = await SendAsync(HttpMethod.Get, SearchUrl(token), subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("   a  ")]
    public async Task Search_RejectsQueryShorterThanTwoCharacters(string? query)
    {
        var world = SeedWorld();
        var url = query is null ? "/api/search" : $"/api/search?q={Uri.EscapeDataString(query)}";

        using var response = await SendAsync(HttpMethod.Get, url, world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await ErrorKeyPresentAsync(response, "q"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("21")]
    [InlineData("-1")]
    [InlineData("abc")]
    public async Task Search_RejectsLimitOutsideOneToTwenty(string limit)
    {
        var world = SeedWorld();
        var token = NewToken();

        using var response = await SendAsync(
            HttpMethod.Get,
            $"{SearchUrl(token)}&limit={limit}",
            world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await ErrorKeyPresentAsync(response, "limit"));
    }

    [Fact]
    public async Task Search_AcceptsLimitBoundsOfOneAndTwenty()
    {
        var world = SeedWorld();
        var token = NewToken();

        using var low = await SendAsync(HttpMethod.Get, $"{SearchUrl(token)}&limit=1", world.SubjectFor("admin"));
        using var high = await SendAsync(HttpMethod.Get, $"{SearchUrl(token)}&limit=20", world.SubjectFor("admin"));

        Assert.Equal(HttpStatusCode.OK, low.StatusCode);
        Assert.Equal(HttpStatusCode.OK, high.StatusCode);
    }

    [Fact]
    public async Task Search_TrimsTheQuery()
    {
        var world = SeedWorld();
        var token = NewToken();
        AddClinic($"Clinic {token}", "1 Main Street");

        var json = await GetJsonAsync($"/api/search?q=%20%20{token}%20%20", world.SubjectFor("admin"));

        Assert.Single(json.GetProperty("clinics").EnumerateArray());
    }

    [Fact]
    public async Task Search_DefaultsToFiveResultsPerList()
    {
        var world = SeedWorld();
        var token = NewToken();
        for (var i = 0; i < 7; i++)
            AddClinic($"Clinic {token} {i:D2}", "1 Main Street");

        var defaulted = await GetJsonAsync(SearchUrl(token), world.SubjectFor("admin"));
        var capped = await GetJsonAsync($"{SearchUrl(token)}&limit=2", world.SubjectFor("admin"));
        var all = await GetJsonAsync($"{SearchUrl(token)}&limit=20", world.SubjectFor("admin"));

        Assert.Equal(5, defaulted.GetProperty("clinics").GetArrayLength());
        Assert.Equal(2, capped.GetProperty("clinics").GetArrayLength());
        Assert.Equal(7, all.GetProperty("clinics").GetArrayLength());
    }

    [Fact]
    public async Task Search_OrdersEachListByNameIgnoringCase()
    {
        var world = SeedWorld();
        var token = NewToken();
        AddClinic($"{token} Charlie", null);
        AddClinic($"{token} alpha", null);
        AddClinic($"{token} Bravo", null);

        var json = await GetJsonAsync(SearchUrl(token), world.SubjectFor("admin"));

        var names = json.GetProperty("clinics").EnumerateArray().Select(item => item.GetProperty("name").GetString());
        Assert.Equal(new[] { $"{token} alpha", $"{token} Bravo", $"{token} Charlie" }, names);
    }

    [Fact]
    public async Task Search_MatchesClinicAddressCaseInsensitively()
    {
        var world = SeedWorld();
        var token = NewToken();
        AddClinic("Plain Name", $"42 {token.ToUpperInvariant()} Lane");

        var json = await GetJsonAsync(SearchUrl(token.ToLowerInvariant()), world.SubjectFor("doctor"));

        var clinic = Assert.Single(json.GetProperty("clinics").EnumerateArray());
        Assert.Equal("Plain Name", clinic.GetProperty("name").GetString());
        Assert.Equal($"42 {token.ToUpperInvariant()} Lane", clinic.GetProperty("address").GetString());
    }

    [Fact]
    public async Task Search_ProvidersAreVerifiedOnlyForNonAdministrators()
    {
        var world = SeedWorld();
        var token = NewToken();
        var verified = Provider.Create(Guid.NewGuid(), world.Clinic.Id, token, "Verified", "Cardiology");
        var pending = Provider.Register(Guid.NewGuid(), world.Clinic.Id, token, "Pending", "Cardiology");
        Factory.Store.Providers.Add(verified);
        Factory.Store.Providers.Add(pending);

        var admin = await GetJsonAsync(SearchUrl(token), world.SubjectFor("admin"));
        var doctor = await GetJsonAsync(SearchUrl(token), world.DoctorSubject);
        var patient = await GetJsonAsync(SearchUrl(token), world.PatientSubject);

        Assert.Equal(2, admin.GetProperty("providers").GetArrayLength());
        Assert.Equal(new[] { verified.Id }, ProviderIds(doctor));
        Assert.Equal(new[] { verified.Id }, ProviderIds(patient));
    }

    [Fact]
    public async Task Search_ProviderClinicNameIsTheClinicName()
    {
        var world = SeedWorld();
        var token = NewToken();
        Factory.Store.Providers.Add(Provider.Create(Guid.NewGuid(), world.Clinic.Id, token, "Clinic", "Surgery"));

        var json = await GetJsonAsync(SearchUrl(token), world.DoctorSubject);

        var provider = Assert.Single(json.GetProperty("providers").EnumerateArray());
        Assert.Equal("Riverside Clinic", provider.GetProperty("clinicName").GetString());
    }

    [Fact]
    public async Task Search_AdministratorSeesEveryPatient()
    {
        var world = SeedWorld();
        var token = NewToken();
        var unrelated = AddPatient(token, "Unrelated", $"{token}@example.com");

        var json = await GetJsonAsync(SearchUrl(token), world.SubjectFor("admin"));

        var patient = Assert.Single(json.GetProperty("patients").EnumerateArray());
        Assert.Equal(unrelated.Id, patient.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Search_DoctorSeesOnlyPatientsWithAnAppointmentWithThem()
    {
        var world = SeedWorld();
        var token = NewToken();
        var ownPatient = AddPatient(token, "Mine", $"mine-{token}@example.com");
        var otherPatient = AddPatient(token, "Theirs", $"theirs-{token}@example.com");
        AddAppointment(ownPatient, world.Doctor);
        AddAppointment(otherPatient, world.OtherDoctor);

        var doctor = await GetJsonAsync(SearchUrl(token), world.DoctorSubject);
        var otherDoctor = await GetJsonAsync(SearchUrl(token), world.OtherDoctorSubject);
        var admin = await GetJsonAsync(SearchUrl(token), world.SubjectFor("admin"));

        Assert.Equal(new[] { ownPatient.Id }, PatientIds(doctor));
        Assert.Equal(new[] { otherPatient.Id }, PatientIds(otherDoctor));
        Assert.Equal(2, admin.GetProperty("patients").GetArrayLength());
    }

    [Fact]
    public async Task Search_ClinicAdministratorSeesPatientsAtTheirClinicsOnly()
    {
        var world = SeedWorld();
        var token = NewToken();
        var riversidePatient = AddPatient(token, "Riverside", $"riverside-{token}@example.com");
        var hillsidePatient = AddPatient(token, "Hillside", $"hillside-{token}@example.com");
        AddAppointment(riversidePatient, world.Doctor);
        AddAppointment(hillsidePatient, world.OtherDoctor);

        var riverside = await GetJsonAsync(SearchUrl(token), world.ClinicAdminSubject);
        var hillside = await GetJsonAsync(SearchUrl(token), world.OtherClinicAdminSubject);

        Assert.Equal(new[] { riversidePatient.Id }, PatientIds(riverside));
        Assert.Equal(new[] { hillsidePatient.Id }, PatientIds(hillside));
    }

    [Fact]
    public async Task Search_PatientsAreEmptyForPatientsAndDoctorsWithoutAppointments()
    {
        var world = SeedWorld();
        var token = NewToken();
        var patient = AddPatient(token, "Hidden", $"hidden-{token}@example.com");
        AddAppointment(patient, world.Doctor);
        AddClinic($"Clinic {token}", null);

        var asPatient = await GetJsonAsync(SearchUrl(token), world.PatientSubject);
        var asOtherDoctor = await GetJsonAsync(SearchUrl(token), world.OtherDoctorSubject);

        Assert.Empty(asPatient.GetProperty("patients").EnumerateArray());
        Assert.Empty(asOtherDoctor.GetProperty("patients").EnumerateArray());
        Assert.Single(asPatient.GetProperty("clinics").EnumerateArray());
    }

    [Fact]
    public async Task Search_PatientMatchesFullNameAndEmailAndShowsEmailAsSubtitle()
    {
        var world = SeedWorld();
        var token = NewToken();
        var patient = AddPatient(token, "Lovelace", $"{token}@example.com");
        var admin = world.SubjectFor("admin");

        var byFullName = await GetJsonAsync(SearchUrl($"{token} Lovelace"), admin);
        var byEmail = await GetJsonAsync(SearchUrl($"{token}@EXAMPLE.com"), admin);

        var match = Assert.Single(byFullName.GetProperty("patients").EnumerateArray());
        Assert.Equal($"{token} Lovelace", match.GetProperty("name").GetString());
        Assert.Equal($"{token}@example.com", match.GetProperty("subtitle").GetString());
        Assert.Equal(new[] { patient.Id }, PatientIds(byEmail));
    }

    [Fact]
    public async Task Search_ReturnsEmptyListsWhenNothingMatches()
    {
        var world = SeedWorld();

        var json = await GetJsonAsync(SearchUrl(NewToken()), world.SubjectFor("admin"));

        Assert.Empty(json.GetProperty("patients").EnumerateArray());
        Assert.Empty(json.GetProperty("providers").EnumerateArray());
        Assert.Empty(json.GetProperty("clinics").EnumerateArray());
    }

    private static string NewToken() => "Tok" + Guid.NewGuid().ToString("N")[..10];

    private static string SearchUrl(string query) => $"/api/search?q={Uri.EscapeDataString(query)}";

    private Patient AddPatient(string firstName, string lastName, string email)
    {
        var patient = Patient.Create(
            Guid.NewGuid(), firstName, lastName, new DateOnly(1990, 1, 1), email, "555-0199");
        Factory.Store.Patients.Add(patient);
        return patient;
    }

    private void AddClinic(string name, string? address)
    {
        Factory.Store.Clinics.Add(Clinic.Create(Guid.NewGuid(), name, address, "555-0198"));
    }

    private void AddAppointment(Patient patient, Provider provider)
    {
        Factory.Store.Appointments.Add(Appointment.Create(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(3),
            "Visit",
            patient,
            provider,
            DateTimeOffset.UtcNow));
    }

    private static List<Guid> PatientIds(JsonElement json) =>
        json.GetProperty("patients").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();

    private static List<Guid> ProviderIds(JsonElement json) =>
        json.GetProperty("providers").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).ToList();

    private static async Task<bool> ErrorKeyPresentAsync(HttpResponseMessage response, string key)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("errors", out var errors)
            && errors.TryGetProperty(key, out _);
    }

    private async Task<JsonElement> GetJsonAsync(string url, string subject)
    {
        using var response = await SendAsync(HttpMethod.Get, url, subject);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
