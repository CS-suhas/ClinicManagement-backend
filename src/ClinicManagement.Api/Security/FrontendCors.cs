namespace ClinicManagement.Api.Security;

public static class FrontendCors
{
    public const string PolicyName = "Frontend";
    public const string SettingName = "CORS_ALLOWED_ORIGINS";
    public const string DevelopmentOrigin = "http://localhost:3000";

    public static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];
    public static readonly string[] AllowedHeaders = ["Authorization", "Content-Type"];

    public static string[] ResolveAllowedOrigins(string? configured, bool isDevelopment)
    {
        var origins = configured?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        if (origins.Length == 0)
        {
            if (isDevelopment)
            {
                return [DevelopmentOrigin];
            }

            throw new InvalidOperationException(
                $"'{SettingName}' must list the explicit frontend origin(s) outside Development.");
        }

        foreach (var origin in origins)
        {
            if (origin.Contains('*'))
            {
                throw new InvalidOperationException($"'{SettingName}' must not contain wildcards.");
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || uri.GetLeftPart(UriPartial.Authority) != origin)
            {
                throw new InvalidOperationException(
                    $"'{SettingName}' entry '{origin}' must be an absolute http(s) origin without a path.");
            }
        }

        return origins;
    }
}
