using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Address { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    
    public ICollection<Provider> Providers { get; private set; } = new List<Provider>();

    private Clinic() { }

    public static Clinic Create(Guid id, string name, string? address, string phoneNumber)
    {
        var clinic = Create(name, address, phoneNumber);
        clinic.Id = id;
        return clinic;
    }

    public static Clinic Create(string name, string? address, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Clinic name cannot be null or empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainValidationException("Clinic phone number cannot be null or empty.", nameof(phoneNumber));

        return new Clinic
        {
            Name = name.Trim(),
            Address = address?.Trim(),
            PhoneNumber = phoneNumber.Trim()
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

    public void SetPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainValidationException("Clinic phone number cannot be empty.", nameof(phoneNumber));
        PhoneNumber = phoneNumber.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}