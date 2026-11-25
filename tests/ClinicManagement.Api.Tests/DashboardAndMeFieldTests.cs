using System.Net;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests;

/// <summary>Dashboard additions (pending counts, recent decisions) and the clinic on the linked provider.</summary>
public sealed class DashboardAndMeFieldTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    [Fact]
    public async Task Dashboard_AdminSectionCountsAllPendingJoinRequests()
    {
        var world = SeedWorld();
        var expected = Factory.Store.JoinRequests.Count(request => request.Status == ClinicJoinRequestStatus.PENDING);

        var section = await GetDashboardSectionAsync(world.SubjectFor("admin"), "admin");

        Assert.True(expected >= 1);
        Assert.Equal(expected, section.GetProperty("pendingJoinRequestCount").GetInt32());
    }

    [Fact]
    public async Task Dashboard_DoctorSectionCountsOnlyRequestsStartingNowOrLater()
    {
        var world = SeedWorld();
        var now = DateTimeOffset.UtcNow;
        // A request that already started must not be counted.
        Factory.Store.Appointments.Add(Appointment.Create(
            Guid.NewGuid(),
            world.Patient.Id,
            world.Doctor.Id,
            now.AddDays(-2),
            "Past request",
            now.AddDays(-3)));
        var expected = Factory.Store.Appointments.Count(appointment =>
            appointment.ProviderId == world.Doctor.Id
            && appointment.Status == AppointmentStatus.REQUESTED
            && appointment.AppointmentDateTime >= now);

        var section = await GetDashboardSectionAsync(world.DoctorSubject, "doctor");

        Assert.True(expected >= 1);
        Assert.Equal(expected, section.GetProperty("pendingRequestCount").GetInt32());
    }

    [Fact]
    public async Task Dashboard_PatientRecentDecisionsListConfirmedAndDeclinedWithDecisionTime()
    {
        var world = SeedWorld();
        var confirm = await SendAsync(HttpMethod.Post, $"/api/appointments/{world.Appointment.Id}/confirm", world.DoctorSubject);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var section = await GetDashboardSectionAsync(world.PatientSubject, "patient");
        var decisions = section.GetProperty("recentDecisions").EnumerateArray().ToList();

        var decision = decisions.Single(item => item.GetProperty("appointmentId").GetGuid() == world.Appointment.Id);
        Assert.Equal("CONFIRMED", decision.GetProperty("status").GetString());
        Assert.Equal("Alan Turing", decision.GetProperty("providerName").GetString());
        Assert.NotEqual(JsonValueKind.Null, decision.GetProperty("decidedAtUtc").ValueKind);
        Assert.True(decisions.Count <= 5);
    }

    [Fact]
    public async Task Dashboard_PatientRecentDecisionsExcludeRequests()
    {
        var world = SeedWorld();
        var section = await GetDashboardSectionAsync(world.PatientSubject, "patient");

        Assert.DoesNotContain(
            section.GetProperty("recentDecisions").EnumerateArray(),
            item => item.GetProperty("appointmentId").GetGuid() == world.Appointment.Id);
    }

    [Fact]
    public async Task Me_ReportsClinicIdOfLinkedProvider()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me", world.DoctorSubject);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var provider = document.RootElement.GetProperty("provider");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(world.Clinic.Id, provider.GetProperty("clinicId").GetGuid());
    }

    [Fact]
    public async Task Me_ReportsClinicIdOfPendingProvider()
    {
        var world = SeedWorld();

        using var response = await SendAsync(HttpMethod.Get, "/api/me", world.PendingSubject);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var provider = document.RootElement.GetProperty("provider");

        Assert.Equal(world.OtherClinic.Id, provider.GetProperty("clinicId").GetGuid());
    }

    private async Task<JsonElement> GetDashboardSectionAsync(string subject, string section)
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/me/dashboard", subject);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty(section).Clone();
    }
}
