namespace ClinicManagement.Application.Common;

public static class Roles
{
    public const string Patient = "patient";
    public const string Doctor = "doctor";
    public const string ClinicAdmin = "clinicadmin";
    public const string Admin = "admin";

    public static readonly IReadOnlySet<string> Assignable = new HashSet<string>(StringComparer.Ordinal) { Admin };

    public static readonly IReadOnlySet<string> Derived =
        new HashSet<string>(StringComparer.Ordinal) { Patient, Doctor, ClinicAdmin };
}
