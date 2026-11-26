using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Repositories;

public interface IAppointmentRepository
{
    Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId);
    Task<IEnumerable<Appointment>> GetAllAppointmentsAsync();
    Task<Guid> AddAppointmentAsync(Appointment request, Guid patientId, Guid providerId);
    Task<Guid> UpdateAppointmentAsync(Appointment request, Guid patientId, Guid providerId);
    Task<Guid> DeleteAppointmentAsync(Guid appointmentId);
    Task<IEnumerable<Appointment>> GetAppointmentsByProviderIdAsync(Guid providerId);
    Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(Guid patientId);
    Task<IEnumerable<Appointment>> GetAppointmentsByClinicIdAsync(Guid clinicId);
    Task<(IReadOnlyList<AppointmentListResponse> Items, int TotalCount)> SearchAppointmentsAsync(
        AppointmentSearchQuery query,
        int offset);
    Task<IReadOnlyList<AppointmentListResponse>> GetUpcomingAppointmentsAsync(DateTimeOffset from, int count);
    /// <summary>Returns false when the stored status is no longer <paramref name="expected"/>.</summary>
    Task<bool> TryUpdateStatusAsync(Guid appointmentId, AppointmentStatus expected, AppointmentStatus next);
    /// <summary>Appointments starting in [start, end). Declined and cancelled ones are excluded unless requested.</summary>
    Task<IReadOnlyList<AppointmentListResponse>> GetProviderAppointmentsInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeInactive);
    /// <summary>Start times of the provider's REQUESTED or CONFIRMED appointments starting in [start, end), earliest first.</summary>
    Task<IReadOnlyList<DateTimeOffset>> GetActiveStartTimesInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end);
    Task<bool> HasProviderAppointmentOverlapAsync(
        Guid providerId,
        DateTimeOffset appointmentStart,
        DateTimeOffset appointmentEnd,
        Guid? excludedAppointmentId);
}