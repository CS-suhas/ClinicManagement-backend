using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Patient : BaseEntity
{
    private readonly List<PatientAllergy> _allergies = new();
    private readonly List<Appointment> _appointments = new();

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public IReadOnlyCollection<PatientAllergy> Allergies => _allergies.AsReadOnly();
    public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

    private Patient() { }

    public static Patient Create(Guid id, string firstName, string lastName, DateOnly dateOfBirth, string email, string phoneNumber)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Patient ID must be a non-empty GUID.", nameof(id));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainValidationException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainValidationException("Last name cannot be empty.", nameof(lastName));

        if (!DataValidator.IsValidDateOfBirth(dateOfBirth))
            throw new DomainValidationException("Date of birth cannot be in the future.", nameof(dateOfBirth));

        if (!DataValidator.IsValidEmail(email))
            throw new DomainValidationException("Email is invalid.", nameof(email));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainValidationException("Phone number cannot be empty.", nameof(phoneNumber));

        return new Patient
        {
            Id = id,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            Email = email.Trim(),
            PhoneNumber = phoneNumber.Trim()
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

    public void SetDateOfBirth(DateOnly dateOfBirth)
    {
        if (!DataValidator.IsValidDateOfBirth(dateOfBirth))
            throw new DomainValidationException("Date of birth cannot be in the future.", nameof(dateOfBirth));

        DateOfBirth = dateOfBirth;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetEmail(string email)
    {
        if (!DataValidator.IsValidEmail(email))
            throw new DomainValidationException("Email is invalid.", nameof(email));

        Email = email.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainValidationException("Phone number cannot be empty.", nameof(phoneNumber));

        PhoneNumber = phoneNumber.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignUser(Guid userId)
    {
        if (!DataValidator.IsValidGuid(userId))
            throw new DomainValidationException("User ID must be a non-empty GUID.", nameof(userId));

        UserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddAllergy(AllergyType type, AllergySeverity severity, string? notes)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_allergies.Any(a => a.AllergyType.Id == type.Id))
            throw new DuplicatePatientAllergyException();

        _allergies.Add(PatientAllergy.Create(Guid.NewGuid(), this, type, severity, notes));
    }

}
