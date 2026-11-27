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
    Task<bool> TryUpdateStatusAsync(Guid appointmentId, AppointmentStatus expected, AppointmentStatus next);
    Task<IReadOnlyList<AppointmentListResponse>> GetProviderAppointmentsInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeInactive);
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