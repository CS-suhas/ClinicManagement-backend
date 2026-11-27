using ClinicManagement.Application.Models;

namespace ClinicManagement.Api.Security;

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
