namespace ClinicManagement.Application.Models;

/// <summary>Represents an allergy recorded for a patient.</summary>
public record PatientAllergyResponse(
    Guid Id,
    Guid AllergyId,
    string Name,
    string Severity,
    string? Notes
);
