using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    
    public ICollection<Provider> Providers { get; private set; } = new List<Provider>();

    private Clinic() { }

    public static Clinic Create(Guid id, string name, string? address, string? phoneNumber)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Clinic ID must be a non-empty GUID.", nameof(id));

        var clinic = Create(name, address, phoneNumber);
        clinic.Id = id;
        return clinic;
    }

    public static Clinic Create(string name, string? address, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Clinic name cannot be null or empty.", nameof(name));
        return new Clinic
        {
            Name = name.Trim(),
            Address = address?.Trim(),
            PhoneNumber = NormalizePhoneNumber(phoneNumber)
        };
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Clinic name cannot be empty.", nameof(name));
        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetAddress(string? address)
    {
        Address = address?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPhoneNumber(string? phoneNumber)
    {
        PhoneNumber = NormalizePhoneNumber(phoneNumber);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? NormalizePhoneNumber(string? phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
}