namespace ClinicManagement.Application.Models;

public record ProviderBusySlotsResponse(
    Guid ProviderId,
    DateOnly Date,
    int AppointmentDurationMinutes,
    IReadOnlyList<DateTimeOffset> BusyStartTimes
);
