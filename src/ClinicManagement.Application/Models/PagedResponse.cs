namespace ClinicManagement.Application.Models;

/// <summary>A page of matching items and the total number of matches.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
