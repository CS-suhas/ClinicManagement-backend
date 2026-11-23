using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Provider : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;
    public Clinic? Clinic { get; private set; }
    public Guid? ClinicId { get; private set; }
    public ProviderVerificationStatus VerificationStatus { get; private set; }
    public string? IdentitySubject { get; private set; }
    public decimal? Fee { get; private set; }
    public Gender? Gender { get; private set; }
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
            Specialty = specialty.Trim(),
            VerificationStatus = ProviderVerificationStatus.VERIFIED
        };
    }

    /// <summary>Self-registration: no clinic yet, and hidden until an admin verifies the provider.</summary>
    public static Provider Register(Guid id, string firstName, string lastName, string specialty)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Provider ID must be a non-empty GUID.", nameof(id));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainValidationException("First name cannot be empty.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainValidationException("Last name cannot be empty.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(specialty))
            throw new DomainValidationException("Specialty cannot be empty.", nameof(specialty));

        return new Provider
        {
            Id = id,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Specialty = specialty.Trim(),
            VerificationStatus = ProviderVerificationStatus.PENDING
        };
    }

    public bool IsAcceptingAppointments =>
        VerificationStatus == ProviderVerificationStatus.VERIFIED && ClinicId is not null;

    /// <summary>Admin decision. Allowed for pending providers, and for previously rejected ones.</summary>
    public void Verify() => ChangeVerification(ProviderVerificationStatus.VERIFIED);

    /// <summary>Admin decision. Only pending providers can be rejected.</summary>
    public void Reject() => ChangeVerification(ProviderVerificationStatus.REJECTED);

    private void ChangeVerification(ProviderVerificationStatus next)
    {
        var allowed = next switch
        {
            ProviderVerificationStatus.VERIFIED => VerificationStatus != ProviderVerificationStatus.VERIFIED,
            ProviderVerificationStatus.REJECTED => VerificationStatus == ProviderVerificationStatus.PENDING,
            _ => false
        };
        if (!allowed)
            throw new InvalidStatusTransitionException("Provider", VerificationStatus.ToString(), next.ToString());

        VerificationStatus = next;
        UpdatedAtUtc = DateTime.UtcNow;
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

    /// <summary>Consultation fee in LKR (Sri Lankan rupees). Optional; when set it must be non-negative with at most two decimal places.</summary>
    public void SetFee(decimal? fee)
    {
        if (fee is { } value && (value < 0 || value >= 100_000_000m || decimal.Round(value, 2) != value))
            throw new DomainValidationException(
                "Fee must be between 0 and 99,999,999.99 with at most two decimal places.", nameof(fee));
        Fee = fee;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetGender(Gender? gender)
    {
        if (gender is { } value && !Enum.IsDefined(value))
            throw new DomainValidationException("Gender is invalid.", nameof(gender));
        Gender = gender;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AssignIdentitySubject(string identitySubject)
    {
        if (string.IsNullOrWhiteSpace(identitySubject))
            throw new DomainValidationException("Identity subject cannot be empty.", nameof(identitySubject));

        IdentitySubject = identitySubject.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetClinic(Clinic clinic)
    {
        Clinic = clinic ?? throw new ArgumentNullException(nameof(clinic), "Clinic cannot be null.");
        ClinicId = clinic.Id;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}