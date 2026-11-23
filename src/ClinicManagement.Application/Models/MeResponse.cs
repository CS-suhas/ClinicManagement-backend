namespace ClinicManagement.Application.Models;

public record MeResponse(
    string? Subject,
    IReadOnlyCollection<string> Roles,
    MeLinkedPatient? Patient,
    MeLinkedProvider? Provider,
    string? DisplayName,
    IReadOnlyCollection<Guid> ClinicAdminOf
);
