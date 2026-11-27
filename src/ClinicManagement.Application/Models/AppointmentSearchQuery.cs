using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Models;

public sealed record AppointmentSearchQuery(
    string? Search = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? ClinicId = null,
    Guid? ProviderId = null,
    Guid? PatientId = null,
    int Page = 1,
    int PageSize = 20,
    AppointmentStatus? Status = null,
    Guid[]? ProviderIds = null)
{
    public const int MaxProviderIds = 100;

    public int GetOffset()
    {
        if (Page < 1)
            throw new DomainValidationException("Page must be greater than zero.", nameof(Page));
        if (PageSize is < 1 or > 100)
            throw new DomainValidationException("Page size must be between 1 and 100.", nameof(PageSize));
        var offset = (long)(Page - 1) * PageSize;
        if (offset > int.MaxValue)
            throw new DomainValidationException("Page is too large.", nameof(Page));
        if (ClinicId == Guid.Empty)
            throw new DomainValidationException("Clinic ID cannot be empty.", nameof(ClinicId));
        if (ProviderId == Guid.Empty)
            throw new DomainValidationException("Provider ID cannot be empty.", nameof(ProviderId));
        if (PatientId == Guid.Empty)
            throw new DomainValidationException("Patient ID cannot be empty.", nameof(PatientId));
        if (From.HasValue && To.HasValue && From >= To)
            throw new DomainValidationException("'from' must be earlier than 'to'.", nameof(From));
        if (ProviderIds is not null && ProviderIds.Length > MaxProviderIds)
            throw new DomainValidationException(
                $"At most {MaxProviderIds} provider IDs can be searched at once.",
                nameof(ProviderIds));
        if (ProviderIds?.Contains(Guid.Empty) == true)
            throw new DomainValidationException("Provider IDs cannot be empty.", nameof(ProviderIds));
        return (int)offset;
    }

    public string? NormalizedSearch => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
