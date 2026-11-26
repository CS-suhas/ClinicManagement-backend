namespace ClinicManagement.Application.Services;

/// <summary>
/// Finds the earliest bookable start time. Slots are opening time plus k times the duration. A slot must fit fully
/// inside clinic hours (both bounds inclusive) and must not conflict with a busy start: a busy start T conflicts with
/// a slot start S when T - duration &lt; S &lt; T + duration. All times are UTC.
/// </summary>
internal static class BookingSlotFinder
{
    public static DateTimeOffset? FindFirstAvailableStart(
        DateTimeOffset now,
        DateOnly firstDay,
        int dayCount,
        TimeOnly clinicStart,
        TimeOnly clinicEnd,
        TimeSpan duration,
        IReadOnlyList<DateTimeOffset> busyStarts)
    {
        for (var dayOffset = 0; dayOffset < dayCount; dayOffset++)
        {
            var day = firstDay.AddDays(dayOffset);
            var opening = new DateTimeOffset(day.ToDateTime(clinicStart), TimeSpan.Zero);
            var closing = new DateTimeOffset(day.ToDateTime(clinicEnd), TimeSpan.Zero);

            for (var start = opening; start + duration <= closing; start += duration)
            {
                if (start < now)
                    continue;
                if (!IsBlocked(start, duration, busyStarts))
                    return start;
            }
        }

        return null;
    }

    private static bool IsBlocked(DateTimeOffset start, TimeSpan duration, IReadOnlyList<DateTimeOffset> busyStarts) =>
        busyStarts.Any(busy => busy - duration < start && start < busy + duration);
}
