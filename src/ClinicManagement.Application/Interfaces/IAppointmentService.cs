using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Appointments.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId);
    Task<List<AppointmentResponse>> GetAllAppointmentsAsync();
    Task<PatientResponse> GetPatientByAppointmentIdAsync(Guid appointmentId);
    Task<ProviderResponse> GetProviderByAppointmentIdAsync(Guid appointmentId);
    Task<ClinicResponse> GetClinicByAppointmentIdAsync(Guid appointmentId);
    Task<Guid> AddAppointmentAsync(AppointmentRequest request);
    Task<Guid> UpdateAppointmentAsync(AppointmentRequest request);
    Task<Guid> DeleteAppointmentAsync(Guid appointmentId);
    Task<PagedResponse<AppointmentListResponse>> SearchAppointmentsAsync(AppointmentSearchQuery query);
}