using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace ClinicManagement.Api.Endpoints;

public static class ApiTags
{
    public const string Me = "Me";
    public const string Appointments = "Appointments";
    public const string Patients = "Patients";
    public const string Providers = "Providers";
    public const string Clinics = "Clinics";
    public const string AllergyTypes = "Allergy types";
    public const string Search = "Search";
    public const string ReferenceData = "Reference data";
    public const string Status = "Status";
    public const string UserRoles = "User roles";

    private static readonly (string Name, string Description)[] Descriptions =
    [
        (Me, "The signed-in user: identity, roles, linked profiles and the role-aware dashboard."),
        (Appointments, "Booking, searching and viewing appointments, and moving them through their status lifecycle (REQUESTED, CONFIRMED, DECLINED, CANCELLED, COMPLETED)."),
        (Patients, "Patient records, their recorded allergies and their appointments."),
        (Providers, "Doctors: the public directory, self-registration and verification, calendars, availability and patient rosters."),
        (Clinics, "Clinics, their providers and appointments, and clinic administrators."),
        (AllergyTypes, "The catalogue of allergy types that can be recorded against patients."),
        (Search, "Cross-resource search over patients, providers and clinics."),
        (ReferenceData, "Fixed values and configuration that clients need to build forms."),
        (Status, "API health, platform totals and the administrator dashboard."),
        (UserRoles, "Explicitly granted roles. Roles derived from data (patient, doctor, clinicadmin) are not managed here.")
    ];

    public static IServiceCollection AddClinicManagementOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options => options
            .AddSchemaTransformer((schema, context, _) =>
            {
                var type = context.JsonTypeInfo.Type;
                var enumType = Nullable.GetUnderlyingType(type) ?? type;
                if (enumType.IsEnum && schema.Enum is not null)
                {
                    schema.Type = JsonSchemaType.String;
                    schema.Enum = Enum.GetNames(enumType).Select(name => (JsonNode)JsonValue.Create(name)).ToList();
                }
                return Task.CompletedTask;
            })
            .AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Clinic Management API";
            document.Info.Description =
                "Manages clinics, providers, patients, allergies and appointments. Send a bearer token for " +
                "anything that is not public. Errors are application/problem+json: 400 for validation failures, " +
                "403 when the signed-in user may not act on the resource, 404 for unknown resources and 409 for " +
                "conflicts with the current state. A missing token (401) or a missing permission (403) rejected " +
                "before the endpoint runs has an empty body.";
            document.Tags = new HashSet<OpenApiTag>(
                Descriptions.Select(tag => new OpenApiTag { Name = tag.Name, Description = tag.Description }));
            return Task.CompletedTask;
        }));
}
