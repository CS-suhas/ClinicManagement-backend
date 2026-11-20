namespace ClinicManagement.Application.Models;

/// <summary>Payload for recording or updating a patient's allergy.</summary>
public sealed record PatientAllergyRequest(
    Guid AllergyId,
    string Severity,
    string? Notes);
