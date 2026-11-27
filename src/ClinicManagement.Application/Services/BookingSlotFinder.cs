namespace ClinicManagement.Application.Services;

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
