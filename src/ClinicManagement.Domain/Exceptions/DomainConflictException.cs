namespace ClinicManagement.Domain.Exceptions;

public abstract class DomainConflictException(string message) : Exception(message)
{
}
