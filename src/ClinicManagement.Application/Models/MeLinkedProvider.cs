namespace ClinicManagement.Application.Models;

/// <summary>ClinicId is null while the provider has no clinic.</summary>
public record MeLinkedProvider(Guid Id, string VerificationStatus, Guid? ClinicId);
