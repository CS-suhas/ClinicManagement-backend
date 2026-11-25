using ClinicManagement.Application.Models;

namespace ClinicManagement.Api.Security;

/// <summary>
/// Request-scoped home of the signed-in user's <see cref="UserAccessContext"/>. Filled by
/// <see cref="RoleClaimsTransformation"/> and read by <see cref="CurrentUser"/>, so the access data is loaded once
/// per request no matter how often it is consulted.
/// </summary>
public sealed class UserAccessContextHolder
{
    public string? Subject { get; private set; }
    public UserAccessContext? Context { get; private set; }

    public void Set(string subject, UserAccessContext context)
    {
        Subject = subject;
        Context = context;
    }
}
