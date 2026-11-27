using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public record PatientAllergyResponse(
    Guid Id,
    Guid AllergyId,
    string Name,
    AllergySeverity Severity,
    string? Notes
);
