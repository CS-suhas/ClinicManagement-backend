using ClinicManagement.Application.Interfaces;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Tests;

public sealed class TestCurrentUser : ICurrentUser
{
    public string? Subject { get; init; } = "test-subject";
    public Guid? UserId { get; init; } = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    public bool IsAuthenticated { get; init; } = true;
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public Patient? Patient { get; init; }
    public Provider? Provider { get; init; }
    public IReadOnlyCollection<Guid> AdministeredClinicIds { get; init; } = [];
    public string? DisplayName { get; init; }

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public Task<Patient?> GetPatientAsync() => Task.FromResult(Patient);

    public Task<Provider?> GetProviderAsync() => Task.FromResult(Provider);

    public Task<bool> IsClinicAdminOfAsync(Guid clinicId) =>
        Task.FromResult(AdministeredClinicIds.Contains(clinicId));

    public Task<IReadOnlyList<Guid>> GetAdministeredClinicIdsAsync() =>
        Task.FromResult<IReadOnlyList<Guid>>(AdministeredClinicIds.ToList());
}
