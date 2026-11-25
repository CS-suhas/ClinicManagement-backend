using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Tests;

public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetBySubjectAsync(string identitySubject) =>
        Task.FromResult(Users.FirstOrDefault(user => user.IdentitySubject == identitySubject));

    public Task<User> GetOrCreateAsync(string identitySubject, string? email = null, string? displayName = null)
    {
        var user = Users.FirstOrDefault(candidate => candidate.IdentitySubject == identitySubject);
        if (user is null)
        {
            user = User.Create(Guid.NewGuid(), identitySubject, email, displayName);
            Users.Add(user);
        }
        return Task.FromResult(user);
    }

    public Task GrantPlatformAdminAsync(Guid userId)
    {
        Users.First(user => user.Id == userId).GrantPlatformAdmin();
        return Task.CompletedTask;
    }

    public Task<UserAccessContext> GetAccessContextAsync(Guid userId) =>
        Task.FromResult(UserAccessContext.Empty(userId));
}
