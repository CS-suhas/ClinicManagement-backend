using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface ISearchRepository
{
    Task<IReadOnlyList<SearchPatientResult>> SearchPatientsAsync(string term, int limit, PatientSearchScope scope);

    Task<IReadOnlyList<SearchProviderResult>> SearchProvidersAsync(string term, int limit, bool verifiedOnly);

    Task<IReadOnlyList<SearchClinicResult>> SearchClinicsAsync(string term, int limit);
}

public sealed record PatientSearchScope(bool AllPatients, Guid? ProviderId, IReadOnlyCollection<Guid> ClinicIds)
{
    public static PatientSearchScope All { get; } = new(true, null, []);

    public bool IsEmpty => !AllPatients && ProviderId is null && ClinicIds.Count == 0;
}
