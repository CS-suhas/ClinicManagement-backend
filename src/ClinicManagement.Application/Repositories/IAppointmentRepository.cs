using ClinicManagement.Domain.Entities;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Appointments.Interfaces;

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
    Task<bool> HasProviderAppointmentOverlapAsync(
        Guid providerId,
        DateTimeOffset appointmentStart,
        DateTimeOffset appointmentEnd,
        Guid? excludedAppointmentId);
}