using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class ClinicAdmin : BaseEntity
{
    public Guid ClinicId { get; private set; }
    public string IdentitySubject { get; private set; } = string.Empty;

    private ClinicAdmin() { }

    public static ClinicAdmin Create(Guid id, Guid clinicId, string identitySubject)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Clinic admin ID must be a non-empty GUID.", nameof(id));
        if (!DataValidator.IsValidGuid(clinicId))
            throw new DomainValidationException("Clinic ID must be a non-empty GUID.", nameof(clinicId));
        if (string.IsNullOrWhiteSpace(identitySubject))
            throw new DomainValidationException("Identity subject cannot be empty.", nameof(identitySubject));

        return new ClinicAdmin
        {
            Id = id,
            ClinicId = clinicId,
            IdentitySubject = identitySubject.Trim()
        };
    }
}
