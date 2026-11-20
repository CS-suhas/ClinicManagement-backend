using ClinicManagement.Application.Appointments.Models;

namespace ClinicManagement.Application.Appointments.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId);
    Task<List<AppointmentResponse>> GetAllAppointmentsAsync();
    Task<Guid> AddAppointmentAsync(AppointmentRequest request);
    Task<Guid> UpdateAppointmentAsync(AppointmentRequest request);
    Task<Guid> DeleteAppointmentAsync(Guid appointmentId);
}