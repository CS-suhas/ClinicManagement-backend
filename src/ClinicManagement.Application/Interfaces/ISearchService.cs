using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(string? query, string? limit);
}
