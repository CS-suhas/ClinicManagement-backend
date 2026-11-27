using System.Net;
using System.Text.Json;
using ClinicManagement.Api.Tests.Support;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests;

public sealed class EnumContractTests(ClinicApiFactory factory)
    : ClinicApiTestBase(factory), IClassFixture<ClinicApiFactory>
{
    [Theory]
    [InlineData("AppointmentListResponse", "status", typeof(AppointmentStatus))]
    [InlineData("AppointmentListResponse", "patientHighestAllergySeverity", typeof(AllergySeverity))]
    [InlineData("AppointmentResponse", "status", typeof(AppointmentStatus))]
    [InlineData("PatientAllergyRequest", "severity", typeof(AllergySeverity))]
    [InlineData("PatientAllergyResponse", "severity", typeof(AllergySeverity))]
    [InlineData("PatientListItemResponse", "highestSeverity", typeof(AllergySeverity))]
    [InlineData("ProviderRequest", "gender", typeof(Gender))]
    [InlineData("ProviderResponse", "gender", typeof(Gender))]
    [InlineData("ProviderResponse", "verificationStatus", typeof(ProviderVerificationStatus))]
    [InlineData("MeLinkedProvider", "verificationStatus", typeof(ProviderVerificationStatus))]
    public async Task OpenApi_ListsEnumValuesForEnumFields(string schemaName, string property, Type enumType)
    {
        using var response = await SendAsync(HttpMethod.Get, "/openapi/v1.json", subject: null);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        var propertySchema = schemas.GetProperty(schemaName).GetProperty("properties").GetProperty(property);
        var enumSchema = Resolve(schemas, propertySchema);
        Assert.Equal("string", enumSchema.GetProperty("type").GetString());
        var values = enumSchema.GetProperty("enum").EnumerateArray()
            .Select(value => value.GetString())
            .ToArray();

        Assert.Equal(Enum.GetNames(enumType), values);
    }

    [Fact]
    public async Task AllergySeverities_ReturnsNamesAndIsDocumentedAsTheEnum()
    {
        using var response = await SendAsync(HttpMethod.Get, "/api/allergy-severities", subject: null);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            Enum.GetNames<AllergySeverity>(),
            body.RootElement.EnumerateArray().Select(value => value.GetString()).ToArray());

        using var openApi = await SendAsync(HttpMethod.Get, "/openapi/v1.json", subject: null);
        using var document = JsonDocument.Parse(await openApi.Content.ReadAsStringAsync());
        var items = document.RootElement.GetProperty("paths").GetProperty("/api/allergy-severities")
            .GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema").GetProperty("items");
        Assert.Equal("#/components/schemas/AllergySeverity", items.GetProperty("$ref").GetString());
    }

    [Fact]
    public async Task AddAllergy_UnknownSeverityIsABadRequestProblem()
    {
        var world = SeedWorld();
        var allergyType = AllergyType.Create("Dust");
        Factory.Store.AllergyTypes.Add(allergyType);

        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/patients/{world.Patient.Id}/allergies",
            world.PatientSubject,
            new { allergyId = allergyType.Id, severity = "EXTREME" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("severity", out _));
        Assert.Empty(world.Patient.Allergies);
    }

    [Fact]
    public async Task AddAllergy_NumericSeverityIsRejected()
    {
        var world = SeedWorld();
        var allergyType = AllergyType.Create("Dust");
        Factory.Store.AllergyTypes.Add(allergyType);

        using var response = await SendAsync(
            HttpMethod.Post,
            $"/api/patients/{world.Patient.Id}/allergies",
            world.PatientSubject,
            new { allergyId = allergyType.Id, severity = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static JsonElement Resolve(JsonElement schemas, JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var reference))
            return schemas.GetProperty(reference.GetString()!.Split('/')[^1]);
        if (schema.TryGetProperty("oneOf", out var oneOf) || schema.TryGetProperty("anyOf", out oneOf))
            return oneOf.EnumerateArray()
                .Select(option => Resolve(schemas, option))
                .First(option => option.TryGetProperty("enum", out _));
        return schema;
    }
}
