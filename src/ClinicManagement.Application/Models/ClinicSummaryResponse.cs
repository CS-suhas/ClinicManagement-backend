namespace ClinicManagement.Application.Models;

public record ClinicSummaryResponse(
    ClinicResponse Clinic,
    int ProviderCount,
    int TodayAppointmentCount,
    IReadOnlyList<string> TopSpecialties,
    IReadOnlyList<ProviderResponse> Providers
);
