namespace ClinicManagement.Api.Security;

/// <summary>Bound from the <c>Authorization</c> section.</summary>
public sealed class AuthorizationSettings
{
    public const string SectionName = "Authorization";

    /// <summary>
    /// Identity subjects to bootstrap as platform administrators. On sign-in a listed subject is granted
    /// <c>Users.IsPlatformAdmin</c>, which then lives in the database: removing a subject from this list does not
    /// revoke the grant.
    /// </summary>
    public string[] AdminSubjects { get; init; } = [];
}
