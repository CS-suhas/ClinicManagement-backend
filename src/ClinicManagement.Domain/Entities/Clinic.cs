using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    
    public ICollection<Provider> Providers { get; private set; } = new List<Provider>();

    private Clinic() { }

    public static Clinic Create(string name, string address, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Clinic name cannot be null or empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Clinic address cannot be null or empty.", nameof(address));
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Clinic phone number cannot be null or empty.", nameof(phoneNumber));

        return new Clinic
        {
            Name = name.Trim(),
            Address = address.Trim(),
            PhoneNumber = phoneNumber.Trim()
        };
    }
}