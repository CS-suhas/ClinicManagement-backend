using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Common;

internal static class RequestValidator
{
    public static void EnsureGuid(Guid value, string propertyName, string displayName)
    {
        if (!DataValidator.IsValidGuid(value))
            throw new DomainValidationException($"{displayName} must be a non-empty GUID.", propertyName);
    }

    public static void EnsureText(string? value, string propertyName, string displayName)
    {
        if (!DataValidator.IsValidText(value))
            throw new DomainValidationException($"{displayName} cannot be empty.", propertyName);
    }

    public static void EnsureEmail(string? value)
    {
        if (!DataValidator.IsValidEmail(value))
            throw new DomainValidationException("Email is invalid.", "email");
    }

    public static void EnsureDateOfBirth(DateOnly value)
    {
        if (!DataValidator.IsValidDateOfBirth(value))
            throw new DomainValidationException("Date of birth cannot be in the future.", "dateOfBirth");
    }

    public static void EnsureFutureDateTime(
        DateTimeOffset value,
        DateTimeOffset currentTime,
        string propertyName)
    {
        if (!DataValidator.IsFutureDateTime(value, currentTime))
            throw new DomainValidationException("Appointment date and time must be in the future.", propertyName);
    }
}
