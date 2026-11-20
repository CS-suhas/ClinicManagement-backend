namespace ClinicManagement.Domain.Exceptions;

public sealed class ProviderAppointmentOverlapException : DomainConflictException
{
    public ProviderAppointmentOverlapException(Guid providerId)
        : base($"Provider '{providerId}' already has an appointment within the selected appointment's duration.")
    {
        ProviderId = providerId;
    }

    public Guid ProviderId { get; }
}
