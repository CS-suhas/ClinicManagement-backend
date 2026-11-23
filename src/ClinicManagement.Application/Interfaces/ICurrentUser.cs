using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Interfaces;

public interface ICurrentUser
{
    string? Subject { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
    Task<Patient?> GetPatientAsync();
    Task<Provider?> GetProviderAsync();
    Task<bool> IsClinicAdminOfAsync(Guid clinicId);
}
