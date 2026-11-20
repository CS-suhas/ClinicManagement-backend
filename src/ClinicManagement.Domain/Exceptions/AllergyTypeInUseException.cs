namespace ClinicManagement.Domain.Exceptions;

public sealed class AllergyTypeInUseException(Guid allergyTypeId)
    : DomainConflictException($"Allergy type '{allergyTypeId}' is recorded for one or more patients and cannot be deleted.")
{
}
