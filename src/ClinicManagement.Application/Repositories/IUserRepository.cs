using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Repositories;

public interface IUserRepository
{
    Task<User?> GetBySubjectAsync(string identitySubject);

    Task<User> GetOrCreateAsync(string identitySubject, string? email = null, string? displayName = null);

    Task<UserAccessContext> GetAccessContextAsync(Guid userId);
}
