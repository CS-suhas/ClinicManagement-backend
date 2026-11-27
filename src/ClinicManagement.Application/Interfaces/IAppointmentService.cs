using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId);
    Task<ProviderBusySlotsResponse> GetProviderBusySlotsAsync(Guid providerId, string? date);
    Task<ProviderSummaryResponse> GetProviderSummaryAsync(Guid providerId);
    Task<List<AppointmentResponse>> GetAllAppointmentsAsync();
    Task<PatientResponse> GetPatientByAppointmentIdAsync(Guid appointmentId);
    Task<ProviderResponse> GetProviderByAppointmentIdAsync(Guid appointmentId);
    Task<ClinicResponse> GetClinicByAppointmentIdAsync(Guid appointmentId);
    Task<Guid> AddAppointmentAsync(AppointmentRequest request);
    Task<Guid> UpdateAppointmentAsync(AppointmentRequest request);
    Task<Guid> DeleteAppointmentAsync(Guid appointmentId);
    Task<PagedResponse<AppointmentListResponse>> SearchAppointmentsAsync(AppointmentSearchQuery query);
    Task ConfirmAppointmentAsync(Guid appointmentId);
    Task DeclineAppointmentAsync(Guid appointmentId);
    Task CancelAppointmentAsync(Guid appointmentId);
    Task CompleteAppointmentAsync(Guid appointmentId);
}