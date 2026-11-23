namespace ClinicManagement.Api.Security;

/// <summary>
/// Which read endpoints anonymous callers may use. Bound from the <c>PublicAccess</c> section
/// (environment: <c>PublicAccess__Doctors</c>) and read on every request, so a change to the
/// configuration source needs no rebuild.
/// </summary>
public sealed class PublicAccessSettings
{
    public const string SectionName = "PublicAccess";

    /// <summary>
    /// When true (the default), the reads behind the public doctor directory that need a sign-in
    /// otherwise (<c>GET /api/clinics</c>) accept anonymous callers. When false they require a
    /// signed-in user, as before. Endpoints that were already public are not affected.
    /// </summary>
    public bool Doctors { get; init; } = true;
}
