using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Models;

public sealed record PageQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20)
{
    public int GetOffset()
    {
        if (Page < 1)
            throw new DomainValidationException("Page must be greater than zero.", nameof(Page));
        if (PageSize is < 1 or > 100)
            throw new DomainValidationException("Page size must be between 1 and 100.", nameof(PageSize));
        var offset = (long)(Page - 1) * PageSize;
        if (offset > int.MaxValue)
            throw new DomainValidationException("Page is too large.", nameof(Page));
        return (int)offset;
    }

    public string? NormalizedSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
