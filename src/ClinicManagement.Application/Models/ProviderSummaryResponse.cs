namespace ClinicManagement.Application.Models;

public record ProviderSummaryResponse(
    ProviderResponse Provider,
    ClinicResponse Clinic,
    DateTimeOffset? NextAvailableStart,
    int TodayAppointmentCount
);
