using System.Globalization;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Services;

public sealed class SearchService(
    ISearchRepository searchRepository,
    ICurrentUser currentUser) : ISearchService
{
    public const int DefaultLimit = 5;
    public const int MaxLimit = 20;
    private const int MinQueryLength = 2;

    public async Task<SearchResponse> SearchAsync(string? query, string? limit)
    {
        currentUser.RequireAuthenticated();

        var term = query?.Trim() ?? string.Empty;
        if (term.Length < MinQueryLength)
            throw new DomainValidationException($"Search text must be at least {MinQueryLength} characters.", "q");
        var take = ParseLimit(limit);

        var isAdministrator = currentUser.IsAdministrator();
        var scope = await ResolvePatientScopeAsync(isAdministrator);

        IReadOnlyList<SearchPatientResult> patients = [];
        if (!scope.IsEmpty)
            patients = await searchRepository.SearchPatientsAsync(term, take, scope);

        var providers = await searchRepository.SearchProvidersAsync(term, take, verifiedOnly: !isAdministrator);
        var clinics = await searchRepository.SearchClinicsAsync(term, take);

        return new SearchResponse(patients, providers, clinics);
    }

    private async Task<PatientSearchScope> ResolvePatientScopeAsync(bool isAdministrator)
    {
        if (isAdministrator)
            return PatientSearchScope.All;

        var provider = await currentUser.GetProviderAsync();
        Guid? providerId = provider is { VerificationStatus: ProviderVerificationStatus.VERIFIED } ? provider.Id : null;
        var clinicIds = (await currentUser.GetAdministeredClinicIdsAsync()).Distinct().ToList();

        return new PatientSearchScope(false, providerId, clinicIds);
    }

    private static int ParseLimit(string? limit)
    {
        if (string.IsNullOrWhiteSpace(limit))
            return DefaultLimit;

        if (!int.TryParse(limit.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            || value is < 1 or > MaxLimit)
        {
            throw new DomainValidationException($"Limit must be between 1 and {MaxLimit}.", "limit");
        }

        return value;
    }
}
