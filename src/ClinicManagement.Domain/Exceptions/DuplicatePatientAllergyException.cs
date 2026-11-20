namespace ClinicManagement.Domain.Exceptions;

public sealed class DuplicatePatientAllergyException : DomainConflictException
{
    public DuplicatePatientAllergyException()
        : base("A patient cannot have the same allergy type assigned more than once.")
    {
    }
}
