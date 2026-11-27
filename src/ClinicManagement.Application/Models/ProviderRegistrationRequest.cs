using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public record ProviderRegistrationRequest(
    Guid Id,
    Guid ClinicId,
    string FirstName,
    string LastName,
    string Specialty,
    decimal? Fee = null,
    Gender? Gender = null
);
