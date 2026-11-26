
namespace ClinicManagement.Application.Models;

/// <summary>Overview of one clinic. Counts use the UTC calendar day for "today".</summary>
public record ClinicSummaryResponse(
    ClinicResponse Clinic,
    int ProviderCount,
    int TodayAppointmentCount,
    IReadOnlyList<string> TopSpecialties,
    IReadOnlyList<ProviderResponse> Providers
);
