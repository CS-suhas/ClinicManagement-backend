using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface ISearchRepository
{
    /// <summary>Patients whose name or email matches <paramref name="term"/>, limited to <paramref name="scope"/>.</summary>
    Task<IReadOnlyList<SearchPatientResult>> SearchPatientsAsync(string term, int limit, PatientSearchScope scope);

    /// <summary>Providers whose name or specialty matches. When <paramref name="verifiedOnly"/>, only verified ones.</summary>
    Task<IReadOnlyList<SearchProviderResult>> SearchProvidersAsync(string term, int limit, bool verifiedOnly);

    /// <summary>Clinics whose name or address matches.</summary>
    Task<IReadOnlyList<SearchClinicResult>> SearchClinicsAsync(string term, int limit);
}

/// <summary>
/// Which patients a caller may find. When <see cref="AllPatients"/> is false, a patient is visible if they have an
/// appointment with <see cref="ProviderId"/> or with a provider at one of <see cref="ClinicIds"/>.
/// </summary>
public sealed record PatientSearchScope(bool AllPatients, Guid? ProviderId, IReadOnlyCollection<Guid> ClinicIds)
{
    public static PatientSearchScope All { get; } = new(true, null, []);

    public bool IsEmpty => !AllPatients && ProviderId is null && ClinicIds.Count == 0;
}
