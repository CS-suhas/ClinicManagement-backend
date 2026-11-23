namespace ClinicManagement.Domain.Exceptions;

public sealed class IdentityAlreadyLinkedException(string resourceName, string identitySubject)
    : DomainConflictException($"The signed-in identity '{identitySubject}' already has a {resourceName}.")
{
}
