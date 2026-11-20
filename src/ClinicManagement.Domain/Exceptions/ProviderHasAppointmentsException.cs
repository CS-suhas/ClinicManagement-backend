namespace ClinicManagement.Domain.Exceptions;

public sealed class ProviderHasAppointmentsException(Guid providerId)
    : DomainConflictException($"Provider '{providerId}' has appointments and cannot be deleted.")
{
}
