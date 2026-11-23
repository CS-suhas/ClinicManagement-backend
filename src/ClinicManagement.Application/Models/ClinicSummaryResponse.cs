using ClinicManagement.Application.Providers.Models;

namespace ClinicManagement.Application.Clinics.Models;

/// <summary>
/// Overview of one clinic. Counts use the UTC calendar day for "today". PendingJoinRequestCount is null unless the
/// caller is an administrator or an administrator of this clinic.
/// </summary>
public record ClinicSummaryResponse(
    ClinicResponse Clinic,
    int ProviderCount,
    int TodayAppointmentCount,
    int? PendingJoinRequestCount,
    IReadOnlyList<string> TopSpecialties,
    IReadOnlyList<ProviderResponse> Providers
);
