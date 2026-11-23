using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Interfaces;

public interface ICurrentUser
{
    string? Subject { get; }
    /// <summary>The signed-in user's <c>Users</c> row id; null when not signed in.</summary>
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    /// <summary>The <c>name</c> claim, else <c>preferred_username</c>; null when neither has a value.</summary>
    string? DisplayName { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
    Task<Patient?> GetPatientAsync();
    Task<Provider?> GetProviderAsync();
    Task<bool> IsClinicAdminOfAsync(Guid clinicId);
    /// <summary>Ids of the clinics the signed-in user administers; empty when none.</summary>
    Task<IReadOnlyList<Guid>> GetAdministeredClinicIdsAsync();
}
