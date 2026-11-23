namespace ClinicManagement.Domain.Exceptions;

public sealed class ClinicJoinRequestAlreadyPendingException(Guid providerId)
    : DomainConflictException($"Provider '{providerId}' already has a pending clinic join request.")
{
}
