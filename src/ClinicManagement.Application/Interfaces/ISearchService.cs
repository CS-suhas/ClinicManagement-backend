using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface ISearchService
{
    /// <summary>
    /// Searches patients, providers and clinics. <paramref name="query"/> is trimmed and must be at least two
    /// characters. <paramref name="limit"/> is 1 to 20 and defaults to 5 when blank.
    /// </summary>
    Task<SearchResponse> SearchAsync(string? query, string? limit);
}
