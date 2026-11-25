using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

/// <summary>
/// A sign-in identity, keyed by the identity provider's subject claim. Patient and provider profiles and
/// clinic-admin assignments hang off a user; a user may have any combination of them.
/// </summary>
public class User : BaseEntity
{
    public string IdentitySubject { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? DisplayName { get; private set; }

    private User() { }

    public static User Create(Guid id, string identitySubject, string? email = null, string? displayName = null)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("User ID must be a non-empty GUID.", nameof(id));
        if (string.IsNullOrWhiteSpace(identitySubject))
            throw new DomainValidationException("Identity subject cannot be empty.", nameof(identitySubject));

        return new User
        {
            Id = id,
            IdentitySubject = identitySubject.Trim(),
            Email = Normalize(email),
            DisplayName = Normalize(displayName)
        };
    }

    /// <summary>
    /// Refreshes the profile from the latest token. Blank values never erase a stored one, so a token that
    /// omits a claim does not wipe it. Returns true when anything changed.
    /// </summary>
    public bool UpdateProfile(string? email, string? displayName)
    {
        var newEmail = Normalize(email) ?? Email;
        var newDisplayName = Normalize(displayName) ?? DisplayName;
        if (newEmail == Email && newDisplayName == DisplayName)
            return false;

        Email = newEmail;
        DisplayName = newDisplayName;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
