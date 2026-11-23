using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class ClinicAdmin : BaseEntity
{
    public Guid ClinicId { get; private set; }
    public Guid UserId { get; private set; }

    private ClinicAdmin() { }

    public static ClinicAdmin Create(Guid id, Guid clinicId, Guid userId)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Clinic admin ID must be a non-empty GUID.", nameof(id));
        if (!DataValidator.IsValidGuid(clinicId))
            throw new DomainValidationException("Clinic ID must be a non-empty GUID.", nameof(clinicId));
        if (!DataValidator.IsValidGuid(userId))
            throw new DomainValidationException("User ID must be a non-empty GUID.", nameof(userId));

        return new ClinicAdmin
        {
            Id = id,
            ClinicId = clinicId,
            UserId = userId
        };
    }
}
