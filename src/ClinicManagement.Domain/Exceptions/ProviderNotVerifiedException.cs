namespace ClinicManagement.Domain.Exceptions;

public sealed class ProviderNotVerifiedException(Guid providerId)
    : DomainConflictException($"Provider '{providerId}' has not been verified.")
{
}
