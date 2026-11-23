namespace ClinicManagement.Application.Models;

/// <summary>Results of a global search. Each list is capped at the requested limit and ordered by name.</summary>
public sealed record SearchResponse(
    IReadOnlyList<SearchPatientResult> Patients,
    IReadOnlyList<SearchProviderResult> Providers,
    IReadOnlyList<SearchClinicResult> Clinics);

/// <summary>A patient match. Name is "First Last" and Subtitle is the email address.</summary>
public sealed record SearchPatientResult(Guid Id, string Name, string Subtitle);

/// <summary>A provider match. ClinicName is null when the provider has no clinic.</summary>
public sealed record SearchProviderResult(Guid Id, string Name, string Specialty, string? ClinicName);

public sealed record SearchClinicResult(Guid Id, string Name, string? Address);
