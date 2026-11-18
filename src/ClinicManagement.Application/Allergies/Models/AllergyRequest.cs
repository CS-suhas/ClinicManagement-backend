using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Allergies.Models;

public record AllergyRequest(
    Guid Id,
    Guid PatientId,
    Guid AllergyTypeId,
    AllergySeverity Severity,
    string? Notes
);
