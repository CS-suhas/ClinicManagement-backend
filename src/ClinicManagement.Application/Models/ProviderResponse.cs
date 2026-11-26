namespace ClinicManagement.Application.Models;

public record ProviderResponse(
    Guid Id,
    Guid ClinicId,
    string FirstName,
    string LastName,
    string Specialty,
    string VerificationStatus,
    decimal? Fee,
    string? Gender,
    string? ClinicName
);
