using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class ClinicJoinRequest : BaseEntity
{
    public Guid ProviderId { get; private set; }
    public Guid ClinicId { get; private set; }
    public ClinicJoinRequestStatus Status { get; private set; } = ClinicJoinRequestStatus.PENDING;
    public DateTimeOffset? DecidedAtUtc { get; private set; }

    private ClinicJoinRequest() { }

    public static ClinicJoinRequest Create(Guid id, Guid providerId, Guid clinicId)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Join request ID must be a non-empty GUID.", nameof(id));
        if (!DataValidator.IsValidGuid(providerId))
            throw new DomainValidationException("Provider ID must be a non-empty GUID.", nameof(providerId));
        if (!DataValidator.IsValidGuid(clinicId))
            throw new DomainValidationException("Clinic ID must be a non-empty GUID.", nameof(clinicId));

        return new ClinicJoinRequest { Id = id, ProviderId = providerId, ClinicId = clinicId };
    }

    public void Approve(DateTimeOffset now) => Decide(ClinicJoinRequestStatus.APPROVED, now);

    public void Reject(DateTimeOffset now) => Decide(ClinicJoinRequestStatus.REJECTED, now);

    public void Cancel(DateTimeOffset now) => Decide(ClinicJoinRequestStatus.CANCELLED, now);

    private void Decide(ClinicJoinRequestStatus next, DateTimeOffset now)
    {
        if (Status != ClinicJoinRequestStatus.PENDING)
            throw new InvalidStatusTransitionException("Clinic join request", Status.ToString(), next.ToString());

        Status = next;
        DecidedAtUtc = now;
        UpdatedAtUtc = now;
    }
}
