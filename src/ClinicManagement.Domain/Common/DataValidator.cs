using System.ComponentModel.DataAnnotations;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Domain.Common;

public static class DataValidator
{
    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && EmailAddressAttribute.IsValid(value.Trim());

    public static bool IsValidGuid(string? value) =>
        Guid.TryParse(value, out var guid) && IsValidGuid(guid);

    public static bool IsValidGuid(Guid value) =>
        value != Guid.Empty;

    public static bool IsValidText(string? value) =>
        !string.IsNullOrWhiteSpace(value);

    public static bool IsValidSeverity(AllergySeverity severity) =>
        Enum.IsDefined(severity);

    public static bool IsValidDateOfBirth(DateOnly value, DateOnly? currentDate = null) =>
        value < (currentDate ?? DateOnly.FromDateTime(DateTime.UtcNow));

    public static bool IsFutureDateTime(DateTimeOffset value, DateTimeOffset currentTime) =>
        value > currentTime;
}
