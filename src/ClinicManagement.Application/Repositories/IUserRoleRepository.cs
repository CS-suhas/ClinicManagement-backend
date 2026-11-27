using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface IUserRoleRepository
{
    Task<bool> AssignAsync(Guid userId, string role, Guid? grantedByUserId);

    Task<bool> RevokeAsync(Guid userId, string role);

    Task<IReadOnlyList<UserRoleAssignment>> ListAsync();
}
