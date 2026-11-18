using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Entities;

public class Patient : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; } // as UTC timestamp
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber {get; private set;} = string.Empty;
    public ICollection<Allergy> Allergies { get; private set; } = new List<Allergy>();
    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    private Patient() { }

    public static Patient Create(Guid id, string firstName, string lastName, DateOnly dateOfBirth, string email, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("First and last names cannot be empty.", nameof(firstName));

        if (dateOfBirth <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Date of birth must be a valid timestamp.", nameof(dateOfBirth));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));

        return new Patient
        {
            Id = id,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth =  dateOfBirth,
            Email = email.Trim(),
            PhoneNumber = phoneNumber.Trim()
        };
    }

    public void SetFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        FirstName = firstName.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetLastName(string lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        LastName = lastName.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetDateOfBirth(DateOnly dateOfBirth)
    {
        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Date of birth cannot be in the future.", nameof(dateOfBirth));

        DateOfBirth = dateOfBirth;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.", nameof(email));

        Email = email.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty.", nameof(phoneNumber));

        PhoneNumber = phoneNumber.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
 
}
