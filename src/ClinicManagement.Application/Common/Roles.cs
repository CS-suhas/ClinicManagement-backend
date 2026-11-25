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
}
