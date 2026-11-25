namespace ClinicManagement.Domain.Exceptions;

public sealed class ProviderAlreadyInClinicException(Guid providerId, Guid clinicId)
    : DomainConflictException($"Provider '{providerId}' already belongs to clinic '{clinicId}'.")
{
}
