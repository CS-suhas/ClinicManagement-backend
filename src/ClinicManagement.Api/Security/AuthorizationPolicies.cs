namespace ClinicManagement.Api.Security;

public static class AuthorizationPolicies
{
    public const string Authenticated = "Authenticated";
    public const string PatientOnly = "PatientOnly";
    public const string DoctorOnly = "DoctorOnly";
    public const string AdminOnly = "AdminOnly";
    public const string ClinicAdminOnly = "ClinicAdminOnly";
    public const string ClinicAdminOrAdministratorOnly = "ClinicAdminOrAdministratorOnly";

    /// <summary>Anonymous while <see cref="PublicAccessSettings.Doctors"/> is on, otherwise signed-in users only.</summary>
    public const string PublicDoctors = "PublicDoctors";

    public const string PatientRole = "patient";
    public const string DoctorRole = "doctor";
    public const string AdminRole = "admin";
    public const string ClinicAdminRole = "clinicadmin";
}
