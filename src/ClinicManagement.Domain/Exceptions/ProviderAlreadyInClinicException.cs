namespace ClinicManagement.Domain.Exceptions;

public sealed class ProviderAlreadyInClinicException(Guid providerId)
    : DomainConflictException($"Provider '{providerId}' already belongs to a clinic.")
{
}
