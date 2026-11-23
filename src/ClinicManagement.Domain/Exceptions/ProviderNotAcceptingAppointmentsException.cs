namespace ClinicManagement.Domain.Exceptions;

public sealed class ProviderNotAcceptingAppointmentsException(Guid providerId)
    : DomainConflictException($"Provider '{providerId}' is not verified or does not belong to a clinic, so appointments cannot be booked.")
{
}
