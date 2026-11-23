namespace ClinicManagement.Domain.Exceptions;

public sealed class InvalidStatusTransitionException(string entityName, string from, string to)
    : DomainConflictException($"{entityName} cannot change from {from} to {to}.")
{
}
