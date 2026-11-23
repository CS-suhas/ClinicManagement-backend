using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Models;

/// <param name="ProviderStatus">Null when the user has no provider profile.</param>
public sealed record UserRoleFacts(
    bool HasPatientProfile,
    ProviderVerificationStatus? ProviderStatus,
    bool IsClinicAdmin,
    bool IsPlatformAdmin);
