using System.Collections.Frozen;
using ClinicManagement.Application.Common;

namespace ClinicManagement.Api.Security;

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
                Permissions.OwnCalendarRead
            ],
            [Roles.ClinicAdmin] =
            [
                Permissions.ProvidersVerify
            ],
            [Roles.Admin] =
            [
                Permissions.AllergyTypesManage,
                Permissions.AppointmentsReadAll,
                Permissions.AppointmentsManage,
                Permissions.ClinicsManage,
                Permissions.ClinicAdminsAssign,
                Permissions.PatientsReadAll,
                Permissions.PatientsManage,
                Permissions.ProvidersReadAll,
                Permissions.ProvidersManage,
                Permissions.ProvidersVerify,
                Permissions.PlatformDashboardRead,
                Permissions.UserRolesManage
            ]
        }.ToFrozenDictionary(
            entry => entry.Key,
            entry => entry.Value.ToFrozenSet(StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);

    public static bool Grants(string role, string permission) =>
        Map.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    public static IReadOnlyList<string> For(IEnumerable<string> roles) =>
        roles.SelectMany(role => Map.TryGetValue(role, out var permissions) ? permissions : [])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
