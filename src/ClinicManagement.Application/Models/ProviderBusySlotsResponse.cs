namespace ClinicManagement.Application.Models;

/// <summary>
/// Public, de-identified view of a provider's booked start times for one UTC date. It carries no patient,
/// appointment or status information. A new booking conflicts with an active appointment that starts strictly
/// within <see cref="AppointmentDurationMinutes"/> before or after it.
/// </summary>
public record ProviderBusySlotsResponse(
    Guid ProviderId,
    DateOnly Date,
    int AppointmentDurationMinutes,
    IReadOnlyList<DateTimeOffset> BusyStartTimes
);
