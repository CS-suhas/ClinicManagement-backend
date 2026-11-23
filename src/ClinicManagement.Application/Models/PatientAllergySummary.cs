namespace ClinicManagement.Application.Models;

/// <summary>How many allergies a patient has recorded, and the most severe one (null when there are none).</summary>
public sealed record PatientAllergySummary(int AllergyCount, string? HighestSeverity);
