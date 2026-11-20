using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class PatientAllergy : BaseEntity
{
    public Patient Patient { get; private set; } = null!;
    public AllergyType AllergyType { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public Guid AllergyId { get; private set; }
    public AllergySeverity Severity { get; private set; }
    public string? Notes { get; private set; }

    private PatientAllergy() { }

    public static PatientAllergy Create(string name, AllergySeverity severity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Allergy name cannot be null or empty.", nameof(name));
        if (!DataValidator.IsValidSeverity(severity))
            throw new DomainValidationException("Allergy severity is invalid.", nameof(severity));

        return new PatientAllergy
        {
            AllergyType = AllergyType.Create(name),
            Severity = severity
        };
    }

    public static PatientAllergy Create(Guid id, Patient patient, AllergyType allergyType, AllergySeverity severity, string? notes)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Patient allergy ID must be a non-empty GUID.", nameof(id));
        if (patient == null)
            throw new ArgumentNullException(nameof(patient), "Patient cannot be null.");
        if (allergyType == null)
            throw new ArgumentNullException(nameof(allergyType), "Allergy type cannot be null.");
        if (!DataValidator.IsValidGuid(patient.Id))
            throw new DomainValidationException("Patient ID must be a non-empty GUID.", nameof(patient));
        if (!DataValidator.IsValidGuid(allergyType.Id))
            throw new DomainValidationException("Allergy type ID must be a non-empty GUID.", nameof(allergyType));
        if (!DataValidator.IsValidSeverity(severity))
            throw new DomainValidationException("Allergy severity is invalid.", nameof(severity));

        return new PatientAllergy
        {
            Id = id,
            Patient = patient,
            AllergyType = allergyType,
            PatientId = patient.Id,
            AllergyId = allergyType.Id,
            Severity = severity,
            Notes = notes?.Trim()
        };
    }

    public void SetSeverity(AllergySeverity severity)
    {
        if (!DataValidator.IsValidSeverity(severity))
            throw new DomainValidationException("Allergy severity is invalid.", nameof(severity));

        Severity = severity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetNotes(string? notes)
    {
        Notes = notes?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPatient(Patient patient)
    {
        Patient = patient ?? throw new ArgumentNullException(nameof(patient), "Patient cannot be null.");
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetAllergyType(AllergyType allergyType)
    {
        AllergyType = allergyType ?? throw new ArgumentNullException(nameof(allergyType), "Allergy type cannot be null.");
        UpdatedAtUtc = DateTime.UtcNow;
    }
}