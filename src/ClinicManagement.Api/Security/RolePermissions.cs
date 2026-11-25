using System.Collections.Frozen;
using ClinicManagement.Application.Common;

namespace ClinicManagement.Api.Security;

/// <summary>
/// The single place that says which role holds which <see cref="Permissions">permission</see>. Granting a role a new
/// ability is a change here, not on the endpoints.
/// </summary>
public static class RolePermissions
{
    private static readonly FrozenDictionary<string, FrozenSet<string>> Map =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [Roles.Patient] =
            [
                Permissions.AppointmentsBook,
                Permissions.OwnPatientProfileEdit
            ],
            [Roles.Doctor] =
            [
                Permissions.AppointmentsRespond,
                Permissions.JoinRequestsSubmit,
                Permissions.OwnCalendarRead
            ],
            [Roles.ClinicAdmin] =
            [
                Permissions.JoinRequestsReview
            ],
            [Roles.Admin] =
            [
                Permissions.AllergyTypesManage,
                Permissions.AppointmentsReadAll,
                Permissions.AppointmentsManage,
                Permissions.ClinicsManage,
                Permissions.ClinicAdminsAssign,
                Permissions.JoinRequestsReview,
                Permissions.PatientsReadAll,
                Permissions.PatientsManage,
                Permissions.ProvidersReadAll,
                Permissions.ProvidersManage,
                Permissions.ProvidersVerify,
                Permissions.PlatformDashboardRead
            ]
        }.ToFrozenDictionary(
            entry => entry.Key,
            entry => entry.Value.ToFrozenSet(StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);

    public static bool Grants(string role, string permission) =>
        Map.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    /// <summary>Every permission held through any of <paramref name="roles"/>, in a stable order.</summary>
    public static IReadOnlyList<string> For(IEnumerable<string> roles) =>
        roles.SelectMany(role => Map.TryGetValue(role, out var permissions) ? permissions : [])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
