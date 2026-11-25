namespace ClinicManagement.Application.Common.Settings;

/// <summary>Bound from the <c>Authorization</c> section.</summary>
public sealed class AuthorizationSettings
{
    public const string SectionName = "Authorization";

    /// <summary>
    /// Identity subjects that are always platform administrators. On sign-in a listed subject is granted the
    /// admin role in <c>UserRoles</c> if they lack it, and that grant cannot be revoked through the API while the
    /// subject is listed. Removing a subject from this list leaves an existing grant in place.
    /// </summary>
    public string[] AdminSubjects { get; init; } = [];

    public bool IsConfiguredAdmin(string identitySubject) =>
        AdminSubjects.Contains(identitySubject, StringComparer.Ordinal);
}
