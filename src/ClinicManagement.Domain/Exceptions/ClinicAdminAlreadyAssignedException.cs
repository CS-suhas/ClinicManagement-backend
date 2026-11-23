namespace ClinicManagement.Domain.Exceptions;

public sealed class ClinicAdminAlreadyAssignedException(string identitySubject, Guid clinicId)
    : DomainConflictException($"Identity '{identitySubject}' already administers clinic '{clinicId}'.")
{
}
