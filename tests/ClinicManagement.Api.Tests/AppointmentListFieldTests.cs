using System.Net;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests;

public sealed class AppointmentListFieldTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    [Fact]
    public async Task SearchItems_ReportAllergyCountAndHighestSeverity()
    {
        var world = SeedWorld();
        world.Patient.AddAllergy(AllergyType.Create("Penicillin"), AllergySeverity.MILD, null);
        world.Patient.AddAllergy(AllergyType.Create("Latex"), AllergySeverity.SEVERE, null);

        var item = await FindItemAsync(world.Appointment.Id, world.SubjectFor("admin"));

        Assert.Equal(2, item.GetProperty("patientAllergyCount").GetInt32());
        Assert.Equal("SEVERE", item.GetProperty("patientHighestAllergySeverity").GetString());
    }

    [Fact]
    public async Task SearchItems_ReportNoSeverityWhenPatientHasNoAllergies()
    {
        var world = SeedWorld();
        var other = Appointment.Create(DateTimeOffset.UtcNow.AddDays(40), "Review", world.OtherPatient, world.OtherDoctor, DateTimeOffset.UtcNow);
        Factory.Store.Appointments.Add(other);

        var item = await FindItemAsync(other.Id, world.SubjectFor("admin"));

        Assert.Equal(0, item.GetProperty("patientAllergyCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("patientHighestAllergySeverity").ValueKind);
    }

    [Fact]
    public async Task SearchItems_StatusChangedAtFallsBackToCreatedAtUntilStatusChanges()
    {
        var world = SeedWorld();
        var before = await FindItemAsync(world.Appointment.Id, world.SubjectFor("admin"));

        Assert.Equal(
            before.GetProperty("createdAtUtc").GetDateTimeOffset(),
            before.GetProperty("statusChangedAtUtc").GetDateTimeOffset());

        using var confirm = await SendAsync(
            HttpMethod.Post,
            $"/api/appointments/{world.Appointment.Id}/confirm",
            world.DoctorSubject);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var after = await FindItemAsync(world.Appointment.Id, world.SubjectFor("admin"));

        Assert.Equal("CONFIRMED", after.GetProperty("status").GetString());
        Assert.True(
            after.GetProperty("statusChangedAtUtc").GetDateTimeOffset()
                >= after.GetProperty("createdAtUtc").GetDateTimeOffset());
    }

    private async Task<JsonElement> FindItemAsync(Guid appointmentId, string subject)
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/appointments/search?pageSize=100", subject);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(element => element.GetProperty("id").GetGuid() == appointmentId)
            .Clone();
    }
}
