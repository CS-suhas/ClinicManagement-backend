using System.Net;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests;

public sealed class PatientAndAllergyFieldTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    [Theory]
    [InlineData("patient")]
    [InlineData("doctor")]
    [InlineData("clinicadmin")]
    public async Task PatientList_IsAdministratorOnly(string role)
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/patients", world.SubjectFor(role));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatientList_RequiresSignIn()
    {
        SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/patients", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatientList_ItemsCarryAllergyCountAndHighestSeverity()
    {
        var world = SeedWorld();
        world.Patient.AddAllergy(AllergyType.Create("Dust"), AllergySeverity.MODERATE, null);
        world.Patient.AddAllergy(AllergyType.Create("Peanut"), AllergySeverity.MILD, null);

        var item = await FindPatientItemAsync("/api/patients", world.Patient.Id, world.SubjectFor("admin"));

        Assert.Equal(2, item.GetProperty("allergyCount").GetInt32());
        Assert.Equal("MODERATE", item.GetProperty("highestSeverity").GetString());
    }

    [Fact]
    public async Task PatientSearch_ItemsCarryAllergySummary()
    {
        var world = SeedWorld();
        world.OtherPatient.AddAllergy(AllergyType.Create("Latex"), AllergySeverity.SEVERE, null);

        using var response = await SendAsync(
            HttpMethod.Get,
            "/api/patients/search?search=Hopper&page=1&pageSize=20",
            world.SubjectFor("admin"));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = document.RootElement.GetProperty("items").EnumerateArray()
            .Single(element => element.GetProperty("id").GetGuid() == world.OtherPatient.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, item.GetProperty("allergyCount").GetInt32());
        Assert.Equal("SEVERE", item.GetProperty("highestSeverity").GetString());
    }

    [Fact]
    public async Task PatientWithoutAllergies_HasZeroCountAndNullSeverity()
    {
        var world = SeedWorld();
        var untouched = Patient.Create(Guid.NewGuid(), "Edith", "Clarke", new DateOnly(1883, 1, 10), "edith@example.com", "555-0199");
        Factory.Store.Patients.Add(untouched);

        var item = await FindPatientItemAsync("/api/patients", untouched.Id, world.SubjectFor("admin"));

        Assert.Equal(0, item.GetProperty("allergyCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("highestSeverity").ValueKind);
    }

    [Fact]
    public async Task ProviderRoster_PatientObjectCarriesAllergySummary()
    {
        var world = SeedWorld();
        world.Patient.AddAllergy(AllergyType.Create("Pollen"), AllergySeverity.SEVERE, null);

        using var response = await SendAsync(
            HttpMethod.Get,
            $"/api/providers/{world.Doctor.Id}/patients?pageSize=100",
            world.DoctorSubject);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var patient = document.RootElement.GetProperty("items").EnumerateArray()
            .Single(row => row.GetProperty("patient").GetProperty("id").GetGuid() == world.Patient.Id)
            .GetProperty("patient");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, patient.GetProperty("allergyCount").GetInt32());
        Assert.Equal("SEVERE", patient.GetProperty("highestSeverity").GetString());
    }

    [Fact]
    public async Task AllergyTypes_ReportNumberOfPatientsWithEachAllergy()
    {
        var world = SeedWorld();
        var allergy = AllergyType.Create("Shellfish");
        Factory.Store.AllergyTypes.Add(allergy);
        world.Patient.AddAllergy(allergy, AllergySeverity.MILD, null);
        world.OtherPatient.AddAllergy(allergy, AllergySeverity.MILD, null);

        using var response = await SendAsync(HttpMethod.Get, $"/api/allergy-types/{allergy.Id}", world.SubjectFor("patient"));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, document.RootElement.GetProperty("patientCount").GetInt32());
    }

    [Fact]
    public async Task AllergyTypes_ListItemsCarryPatientCount()
    {
        var world = SeedWorld();
        var allergy = AllergyType.Create("Mould");
        Factory.Store.AllergyTypes.Add(allergy);

        using var response = await SendAsync(HttpMethod.Get, "/api/allergy-types", world.SubjectFor("patient"));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var item = document.RootElement.EnumerateArray().Single(element => element.GetProperty("id").GetGuid() == allergy.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, item.GetProperty("patientCount").GetInt32());
    }

    [Fact]
    public async Task AllergyTypes_RequireSignIn()
    {
        SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/allergy-types", subject: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<JsonElement> FindPatientItemAsync(string url, Guid patientId, string subject)
    {
        using var response = await SendAsync(HttpMethod.Get, url, subject);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var items = root.ValueKind == JsonValueKind.Array ? root : root.GetProperty("items");
        return items.EnumerateArray().Single(element => element.GetProperty("id").GetGuid() == patientId).Clone();
    }
}
