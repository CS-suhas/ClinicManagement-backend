namespace ClinicManagement.Application.Models;

public sealed record SearchResponse(
    IReadOnlyList<SearchPatientResult> Patients,
    IReadOnlyList<SearchProviderResult> Providers,
    IReadOnlyList<SearchClinicResult> Clinics);

public sealed record SearchPatientResult(Guid Id, string Name, string Subtitle);

public sealed record SearchProviderResult(Guid Id, string Name, string Specialty, string? ClinicName);

public sealed record SearchClinicResult(Guid Id, string Name, string? Address);
