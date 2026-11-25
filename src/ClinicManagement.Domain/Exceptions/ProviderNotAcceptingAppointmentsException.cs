namespace ClinicManagement.Domain.Exceptions;

public sealed class ProviderNotAcceptingAppointmentsException(Guid providerId)
    : DomainConflictException($"Provider '{providerId}' is not verified, so appointments cannot be booked.")
{
}
