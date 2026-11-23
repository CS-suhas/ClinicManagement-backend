using ClinicManagement.Application.Common;
using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Repositories;

namespace ClinicManagement.Application.Services;

public sealed class MeDashboardService(
    IStatusRepository statusRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IMeDashboardService
{
    private const string AdminRole = "admin";
    private const string ClinicAdminRole = "clinicadmin";
    private const string DoctorRole = "doctor";
    private const string PatientRole = "patient";

    public async Task<MeDashboardResponse> GetDashboardAsync()
    {
        CurrentUserGuard.RequireAuthenticated(currentUser);

        // All day and week boundaries are UTC: "today" is [todayStart, todayEnd) and "this week" is the seven
        // days starting at todayStart.
        var now = timeProvider.GetUtcNow();
        var todayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);
        var weekEnd = todayStart.AddDays(7);

        AdminDashboardSection? admin = null;
        if (currentUser.IsInRole(AdminRole))
        {
            var counts = await statusRepository.GetPlatformCountsAsync(todayStart, todayEnd);
            admin = new AdminDashboardSection(
                counts.ClinicCount,
                counts.ProviderCount,
                counts.PatientCount,
                counts.PendingProviderVerificationCount,
                counts.TodayAppointmentCount);
        }

        ClinicAdminDashboardSection? clinicAdmin = null;
        if (currentUser.IsInRole(ClinicAdminRole))
        {
            var clinicIds = await currentUser.GetAdministeredClinicIdsAsync();
            var counts = clinicIds.Count == 0
                ? new DashboardScopeCounts(0, 0, 0, 0, 0)
                : await statusRepository.GetClinicCountsAsync(clinicIds, todayStart, todayEnd);
            var pendingJoinRequests = clinicIds.Count == 0
                ? 0
                : await statusRepository.CountPendingJoinRequestsAsync(clinicIds);
            clinicAdmin = new ClinicAdminDashboardSection(
                counts.ClinicCount,
                counts.ProviderCount,
                counts.PatientCount,
                counts.PendingProviderVerificationCount,
                counts.TodayAppointmentCount,
                pendingJoinRequests);
        }

        DoctorDashboardSection? doctor = null;
        if (currentUser.IsInRole(DoctorRole) && await currentUser.GetProviderAsync() is { } provider)
            doctor = await statusRepository.GetDoctorDashboardAsync(provider.Id, now, todayStart, todayEnd, weekEnd);

        PatientDashboardSection? patient = null;
        if (currentUser.IsInRole(PatientRole) && await currentUser.GetPatientAsync() is { } linkedPatient)
            patient = await statusRepository.GetPatientDashboardAsync(linkedPatient.Id, now);

        return new MeDashboardResponse(admin, clinicAdmin, doctor, patient);
    }
}
