namespace ClinicManagement.Application.Common.Settings;

public sealed class ClinicManagementSettings
{
    public const string SectionName = "ClinicManagementSettings";

    public int DefaultAppointmentDurationMinutes { get; init; } = 20;
    public TimeOnly DefaultClinicStartTime { get; init; } = new(8, 0);
    public TimeOnly DefaultClinicEndTime { get; init; } = new(16, 0);

    private ClinicManagementSettings() { }
}
