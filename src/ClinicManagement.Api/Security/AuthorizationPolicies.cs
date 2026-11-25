namespace ClinicManagement.Api.Security;

/// <summary>Policies that are not about a permission. Role-based access uses <see cref="Permissions"/>.</summary>
public static class AuthorizationPolicies
{
    public const string Authenticated = "Authenticated";

    /// <summary>Anonymous while <see cref="PublicAccessSettings.Doctors"/> is on, otherwise signed-in users only.</summary>
    public const string PublicDoctors = "PublicDoctors";
}
