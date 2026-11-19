using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Appointments.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId);
    Task<IEnumerable<Appointment>> GetAllAppointmentsAsync();
    Task<Guid> AddAppointmentAsync(Appointment request);
    Task<Guid> UpdateAppointmentAsync(Appointment request);
    Task<Guid> DeleteAppointmentAsync(Guid appointmentId);
}