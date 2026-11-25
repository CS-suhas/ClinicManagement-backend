namespace ClinicManagement.Domain.Exceptions;

public sealed class RoleRevocationNotAllowedException(string identitySubject, string role, string reason)
    : DomainConflictException($"Role '{role}' cannot be revoked from identity '{identitySubject}': {reason}")
{
}
