namespace ClinicManagement.Application.Common;

/// <summary>
/// Role names the backend derives for a signed-in user. Roles are never read from the identity provider's token.
/// </summary>
public static class Roles
{
    public const string Patient = "patient";
    public const string Doctor = "doctor";
    public const string ClinicAdmin = "clinicadmin";
    public const string Admin = "admin";

    /// <summary>
    /// Roles an administrator grants explicitly (stored in <c>UserRoles</c>). The others are derived from a user's
    /// data: a patient profile, a verified provider profile, a clinic-admin assignment.
    /// </summary>
    public static readonly IReadOnlySet<string> Assignable = new HashSet<string>(StringComparer.Ordinal) { Admin };

    public static readonly IReadOnlySet<string> Derived =
        new HashSet<string>(StringComparer.Ordinal) { Patient, Doctor, ClinicAdmin };
}
