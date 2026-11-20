using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Provider : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;
    public Clinic Clinic { get; private set; } = null!;
    public Guid ClinicId { get; private set; }
    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    private Provider() { }

    public static Provider Create(Guid id, string firstName, string lastName, string specialty, Clinic clinic = null!)
    {
        ArgumentNullException.ThrowIfNull(clinic);

        var provider = Create(id, clinic.Id, firstName, lastName, specialty);
        provider.Clinic = clinic;
        return provider;
    }

    public static Provider Create(Guid id, Guid clinicId, string firstName, string lastName, string specialty)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Provider ID must be a non-empty GUID.", nameof(id));
        if (!DataValidator.IsValidGuid(clinicId))
            throw new DomainValidationException("Clinic ID must be a non-empty GUID.", nameof(clinicId));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainValidationException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainValidationException("Last name cannot be empty.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(specialty))
            throw new DomainValidationException("Specialty cannot be empty.", nameof(specialty));

        return new Provider
        {
            Id = id,
            ClinicId = clinicId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Specialty = specialty.Trim()
        };
    }

    public void SetFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainValidationException("First name cannot be empty.", nameof(firstName));
        FirstName = firstName.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetLastName(string lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainValidationException("Last name cannot be empty.", nameof(lastName));
        LastName = lastName.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetSpecialty(string specialty)
    {
        if (string.IsNullOrWhiteSpace(specialty))
            throw new DomainValidationException("Specialty cannot be empty.", nameof(specialty));
        Specialty = specialty.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetClinic(Clinic clinic)
    {
        Clinic = clinic ?? throw new ArgumentNullException(nameof(clinic), "Clinic cannot be null.");
        ClinicId = clinic.Id;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}