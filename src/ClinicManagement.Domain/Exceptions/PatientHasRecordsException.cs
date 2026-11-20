namespace ClinicManagement.Domain.Exceptions;

public sealed class PatientHasRecordsException(Guid patientId)
    : DomainConflictException($"Patient '{patientId}' has recorded allergies or appointments and cannot be deleted.")
{
}
