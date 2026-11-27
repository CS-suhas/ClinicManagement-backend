using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests.Support;

internal sealed class InMemorySearchRepository(InMemoryStore store) : ISearchRepository
{
    public Task<IReadOnlyList<SearchPatientResult>> SearchPatientsAsync(
        string term,
        int limit,
        PatientSearchScope scope)
    {
        IEnumerable<ClinicManagement.Domain.Entities.Patient> patients = store.Patients;
        if (!scope.AllPatients)
        {
            var visiblePatientIds = store.Appointments
                .Where(appointment =>
                    (scope.ProviderId is { } providerId && appointment.ProviderId == providerId)
                    || (appointment.Provider.ClinicId is { } clinicId && scope.ClinicIds.Contains(clinicId)))
                .Select(appointment => appointment.PatientId)
                .ToHashSet();
            patients = patients.Where(patient => visiblePatientIds.Contains(patient.Id));
        }

        IReadOnlyList<SearchPatientResult> results = patients
            .Where(patient => Matches(term,
                patient.FirstName,
                patient.LastName,
                $"{patient.FirstName} {patient.LastName}",
                patient.Email))
            .OrderBy(patient => Upper($"{patient.FirstName} {patient.LastName}"), StringComparer.Ordinal)
            .ThenBy(patient => patient.Id)
            .Take(limit)
            .Select(patient => new SearchPatientResult(
                patient.Id,
                $"{patient.FirstName} {patient.LastName}",
                patient.Email))
            .ToList();
        return Task.FromResult(results);
    }

    public Task<IReadOnlyList<SearchProviderResult>> SearchProvidersAsync(string term, int limit, bool verifiedOnly)
    {
        IReadOnlyList<SearchProviderResult> results = store.Providers
            .Where(provider => !verifiedOnly || provider.VerificationStatus == ProviderVerificationStatus.VERIFIED)
            .Where(provider => Matches(term,
                provider.FirstName,
                provider.LastName,
                $"{provider.FirstName} {provider.LastName}",
                provider.Specialty))
            .OrderBy(provider => Upper($"{provider.FirstName} {provider.LastName}"), StringComparer.Ordinal)
            .ThenBy(provider => provider.Id)
            .Take(limit)
            .Select(provider => new SearchProviderResult(
                provider.Id,
                $"{provider.FirstName} {provider.LastName}",
                provider.Specialty,
                store.Clinics.FirstOrDefault(clinic => clinic.Id == provider.ClinicId)?.Name))
            .ToList();
        return Task.FromResult(results);
    }

    public Task<IReadOnlyList<SearchClinicResult>> SearchClinicsAsync(string term, int limit)
    {
        IReadOnlyList<SearchClinicResult> results = store.Clinics
            .Where(clinic => Matches(term, clinic.Name, clinic.Address))
            .OrderBy(clinic => Upper(clinic.Name), StringComparer.Ordinal)
            .ThenBy(clinic => clinic.Id)
            .Take(limit)
            .Select(clinic => new SearchClinicResult(clinic.Id, clinic.Name, clinic.Address))
            .ToList();
        return Task.FromResult(results);
    }

    private static bool Matches(string term, params string?[] fields) =>
        fields.Any(field => field is not null && Upper(field).Contains(Upper(term), StringComparison.Ordinal));

    private static string Upper(string value) => value.ToUpperInvariant();
}
