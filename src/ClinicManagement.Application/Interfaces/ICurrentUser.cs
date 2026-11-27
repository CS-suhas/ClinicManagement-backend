using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Interfaces;

public interface ICurrentUser
{
    string? Subject { get; }
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    string? DisplayName { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
    Task<Patient?> GetPatientAsync();
    Task<Provider?> GetProviderAsync();
    Task<bool> IsClinicAdminOfAsync(Guid clinicId);
    Task<IReadOnlyList<Guid>> GetAdministeredClinicIdsAsync();
}
