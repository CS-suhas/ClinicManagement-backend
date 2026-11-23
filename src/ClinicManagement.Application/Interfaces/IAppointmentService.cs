using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Appointments.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId);
    /// <summary>Public. Booked start times for a provider on a UTC date (YYYY-MM-DD). Unbookable providers are not found.</summary>
    Task<ProviderBusySlotsResponse> GetProviderBusySlotsAsync(Guid providerId, string? date);
    /// <summary>Public. Bookable provider overview with the next free slot in the next 14 UTC days. Unbookable providers are not found.</summary>
    Task<ProviderSummaryResponse> GetProviderSummaryAsync(Guid providerId);
    Task<List<AppointmentResponse>> GetAllAppointmentsAsync();
    Task<PatientResponse> GetPatientByAppointmentIdAsync(Guid appointmentId);
    Task<ProviderResponse> GetProviderByAppointmentIdAsync(Guid appointmentId);
    Task<ClinicResponse> GetClinicByAppointmentIdAsync(Guid appointmentId);
    Task<Guid> AddAppointmentAsync(AppointmentRequest request);
    Task<Guid> UpdateAppointmentAsync(AppointmentRequest request);
    Task<Guid> DeleteAppointmentAsync(Guid appointmentId);
    Task<PagedResponse<AppointmentListResponse>> SearchAppointmentsAsync(AppointmentSearchQuery query);
    /// <summary>Provider accepts a requested appointment.</summary>
    Task ConfirmAppointmentAsync(Guid appointmentId);
    /// <summary>Provider declines a requested appointment.</summary>
    Task DeclineAppointmentAsync(Guid appointmentId);
    /// <summary>Cancels a requested or confirmed appointment, freeing the slot.</summary>
    Task CancelAppointmentAsync(Guid appointmentId);
    Task CompleteAppointmentAsync(Guid appointmentId);
}