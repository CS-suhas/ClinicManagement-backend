using ClinicManagement.Application.Clinics.Models;

namespace ClinicManagement.Application.Providers.Models;

/// <summary>
/// Public overview of a bookable provider. NextAvailableStart is the earliest free slot starting at or after now
/// within the next 14 UTC days, or null when none exists. TodayAppointmentCount counts the provider's appointments
/// on the current UTC day, excluding declined and cancelled ones.
/// </summary>
public record ProviderSummaryResponse(
    ProviderResponse Provider,
    ClinicResponse Clinic,
    DateTimeOffset? NextAvailableStart,
    int TodayAppointmentCount
);
