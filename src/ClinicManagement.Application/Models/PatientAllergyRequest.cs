using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public sealed record PatientAllergyRequest(
    Guid AllergyId,
    AllergySeverity Severity,
    string? Notes);
