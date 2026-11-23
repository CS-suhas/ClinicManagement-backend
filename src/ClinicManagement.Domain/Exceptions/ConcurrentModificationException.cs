namespace ClinicManagement.Domain.Exceptions;

public sealed class ConcurrentModificationException(string entityName, Guid id)
    : DomainConflictException($"{entityName} '{id}' was changed by another request. Reload it and try again.")
{
}
