namespace ClinicManagement.Api.Security;

public sealed class PublicAccessSettings
{
    public const string SectionName = "PublicAccess";

    public bool Doctors { get; init; } = true;
}
