using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface IUserRoleService
{
    Task<IReadOnlyList<UserRoleAssignment>> ListAssignmentsAsync();

    Task<UserRolesResponse> GetUserRolesAsync(string identitySubject);

    Task AssignRoleAsync(string identitySubject, string role);

    Task RevokeRoleAsync(string identitySubject, string role);
}
