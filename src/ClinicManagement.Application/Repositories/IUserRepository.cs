using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetBySubjectAsync(string identitySubject);

    /// <summary>
    /// Returns the user for <paramref name="identitySubject"/>, creating it when missing. An existing user's
    /// email and display name are refreshed from non-blank values.
    /// </summary>
    Task<User> GetOrCreateAsync(string identitySubject, string? email = null, string? displayName = null);

    /// <summary>Everything role resolution needs about one user, in a single round trip.</summary>
    Task<UserRoleFacts> GetRoleFactsAsync(Guid userId);
}
