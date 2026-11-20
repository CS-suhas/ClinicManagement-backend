using ClinicManagement.Domain.Entities;

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
}