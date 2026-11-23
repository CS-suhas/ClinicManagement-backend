using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class PlatformAdmin : BaseEntity
{
    public string IdentitySubject { get; private set; } = string.Empty;

    private PlatformAdmin() { }

    public static PlatformAdmin Create(Guid id, string identitySubject)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Platform admin ID must be a non-empty GUID.", nameof(id));
        if (string.IsNullOrWhiteSpace(identitySubject))
            throw new DomainValidationException("Identity subject cannot be empty.", nameof(identitySubject));

        return new PlatformAdmin
        {
            Id = id,
            IdentitySubject = identitySubject.Trim()
        };
    }
}
