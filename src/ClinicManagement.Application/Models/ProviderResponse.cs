using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public record ProviderResponse(
    Guid Id,
    Guid ClinicId,
    string FirstName,
    string LastName,
    string Specialty,
    ProviderVerificationStatus VerificationStatus,
    decimal? Fee,
    Gender? Gender,
    string? ClinicName
);
