namespace ClinicManagement.Api.Security;

/// <summary>
/// What an endpoint requires, independent of who may do it. Each permission is registered as an authorization
/// policy of the same name; <see cref="RolePermissions"/> decides which roles hold it.
/// </summary>
public static class Permissions
{
    public const string AllergyTypesManage = "allergy-types:manage";

    public const string AppointmentsBook = "appointments:book";
    public const string AppointmentsRespond = "appointments:respond";
    public const string AppointmentsReadAll = "appointments:read-all";
    public const string AppointmentsManage = "appointments:manage";

    public const string ClinicsManage = "clinics:manage";
    public const string ClinicAdminsAssign = "clinic-admins:assign";

    public const string PatientsReadAll = "patients:read-all";
    public const string PatientsManage = "patients:manage";
    public const string OwnPatientProfileEdit = "patients:edit-own";

    public const string ProvidersReadAll = "providers:read-all";
    public const string ProvidersManage = "providers:manage";
    public const string ProvidersVerify = "providers:verify";
    public const string OwnCalendarRead = "providers:read-own-calendar";

    public const string PlatformDashboardRead = "platform:dashboard";

    public const string UserRolesManage = "user-roles:manage";

    public static readonly IReadOnlyList<string> All =
    [
        AllergyTypesManage,
        AppointmentsBook, AppointmentsRespond, AppointmentsReadAll, AppointmentsManage,
        ClinicsManage, ClinicAdminsAssign,
        PatientsReadAll, PatientsManage, OwnPatientProfileEdit,
        ProvidersReadAll, ProvidersManage, ProvidersVerify, OwnCalendarRead,
        PlatformDashboardRead,
        UserRolesManage
    ];
}
