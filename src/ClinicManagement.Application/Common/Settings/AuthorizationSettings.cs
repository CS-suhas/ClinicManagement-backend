namespace ClinicManagement.Application.Common.Settings;

public sealed class AuthorizationSettings
{
    public const string SectionName = "Authorization";

    public string[] AdminSubjects { get; init; } = [];

    public bool IsConfiguredAdmin(string identitySubject) =>
        AdminSubjects.Contains(identitySubject, StringComparer.Ordinal);
}
