namespace ClinicManagement.Application.ClinicJoinRequests.Models;

/// <summary>Join request with provider and clinic details, for approval queues and history.</summary>
public sealed record ClinicJoinRequestResponse(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    string ProviderSpecialty,
    Guid ClinicId,
    string ClinicName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DecidedAtUtc);
