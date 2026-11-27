using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

public record MeLinkedProvider(Guid Id, ProviderVerificationStatus VerificationStatus, Guid ClinicId);
