using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Appointments.Mapping;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Clinics.Mapping;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Patients.Mapping;
using ClinicManagement.Application.Patients.Models;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Mapping;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Application.Models;

namespace ClinicManagement.Application.Appointments.Services;

public class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IClinicRepository clinicRepository,
    IProviderRepository providerRepository,
    ClinicManagementSettings clinicSettings,
    TimeProvider timeProvider) : IAppointmentService
{
    public async Task<Guid> AddAppointmentAsync(AppointmentRequest request)
    {
        var currentTime = timeProvider.GetUtcNow();
        ValidateRequest(request, currentTime);
        EnsureAppointmentWithinClinicHours(request.AppointmentDateTime);

        var patientExists = await patientRepository.PatientExistsAsync(request.PatientId);

        if (!patientExists)
        {
            throw new ResourceNotFoundException("Patient", request.PatientId);
        }

        var provider = await providerRepository.GetProviderByIdAsync(request.ProviderId)
            ?? throw new ResourceNotFoundException("Provider", request.ProviderId);
        if (!provider.IsAcceptingAppointments)
        {
            throw new ProviderNotAcceptingAppointmentsException(request.ProviderId);
        }

        await EnsureNoProviderAppointmentOverlapAsync(
            request.ProviderId,
            request.AppointmentDateTime,
            null);

        var newAppointment = request.ToEntity(currentTime);
        await appointmentRepository.AddAppointmentAsync(newAppointment, request.PatientId, request.ProviderId);
        return request.Id;
    }

    public async Task<Guid> DeleteAppointmentAsync(Guid appointmentId)
    {
        RequestValidator.EnsureGuid(appointmentId, nameof(appointmentId), "Appointment ID");

        if (await appointmentRepository.GetAppointmentByIdAsync(appointmentId) is null)
            throw new ResourceNotFoundException("Appointment", appointmentId);

        return await appointmentRepository.DeleteAppointmentAsync(appointmentId);
    }

    public async Task<List<AppointmentResponse>> GetAllAppointmentsAsync()
    {
        return (await appointmentRepository.GetAllAppointmentsAsync())
            .Select(appointment => appointment.ToResponse())
            .ToList();
    }

    public async Task<PagedResponse<AppointmentListResponse>> SearchAppointmentsAsync(
        AppointmentSearchQuery query)
    {
        var offset = query.GetOffset();
        var (items, totalCount) = await appointmentRepository.SearchAppointmentsAsync(query, offset);
        return new PagedResponse<AppointmentListResponse>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId)
    {
        var appointment = await GetRequiredAppointmentAsync(appointmentId);
        return appointment.ToResponse();
    }

    public async Task<PatientResponse> GetPatientByAppointmentIdAsync(Guid appointmentId) =>
        (await GetRequiredAppointmentAsync(appointmentId)).Patient.ToResponse();

    public async Task<ProviderResponse> GetProviderByAppointmentIdAsync(Guid appointmentId) =>
        (await GetRequiredAppointmentAsync(appointmentId)).Provider.ToResponse();

    public async Task<ClinicResponse> GetClinicByAppointmentIdAsync(Guid appointmentId)
    {
        var appointment = await GetRequiredAppointmentAsync(appointmentId);
        if (appointment.Provider.ClinicId is not { } clinicId)
            throw new ResourceNotFoundException(
                $"Provider '{appointment.ProviderId}' does not belong to a clinic.");
        var clinic = await clinicRepository.GetClinicByIdAsync(clinicId)
            ?? throw new ResourceNotFoundException("Clinic", clinicId);
        return clinic.ToResponse();
    }

    public async Task<Guid> UpdateAppointmentAsync(AppointmentRequest request)
    {
        var currentTime = timeProvider.GetUtcNow();
        ValidateRequest(request, currentTime);
        EnsureAppointmentWithinClinicHours(request.AppointmentDateTime);

        var existingAppointment = await appointmentRepository.GetAppointmentByIdAsync(request.Id)
            ?? throw new ResourceNotFoundException("Appointment", request.Id);

        if (existingAppointment.AppointmentDateTime != request.AppointmentDateTime
            || existingAppointment.ProviderId != request.ProviderId)
        {
            await EnsureNoProviderAppointmentOverlapAsync(
                request.ProviderId,
                request.AppointmentDateTime,
                request.Id);
        }
        return await appointmentRepository.UpdateAppointmentAsync(
            request.ToEntity(currentTime),
            request.PatientId,
            request.ProviderId);
    }

    public Task ConfirmAppointmentAsync(Guid appointmentId) =>
        ChangeStatusAsync(appointmentId, (appointment, now) => appointment.Confirm(now));

    public Task DeclineAppointmentAsync(Guid appointmentId) =>
        ChangeStatusAsync(appointmentId, (appointment, now) => appointment.Decline(now));

    public Task CancelAppointmentAsync(Guid appointmentId) =>
        ChangeStatusAsync(appointmentId, (appointment, now) => appointment.Cancel(now));

    public Task CompleteAppointmentAsync(Guid appointmentId) =>
        ChangeStatusAsync(appointmentId, (appointment, now) => appointment.Complete(now));

    private async Task ChangeStatusAsync(Guid appointmentId, Action<Appointment, DateTimeOffset> transition)
    {
        var appointment = await GetRequiredAppointmentAsync(appointmentId);
        var expected = appointment.Status;
        transition(appointment, timeProvider.GetUtcNow());

        if (!await appointmentRepository.TryUpdateStatusAsync(appointmentId, expected, appointment.Status))
            throw new ConcurrentModificationException("Appointment", appointmentId);
    }

    private static void ValidateRequest(AppointmentRequest request, DateTimeOffset currentTime)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Appointment ID");
        RequestValidator.EnsureGuid(request.PatientId, "patientId", "Patient ID");
        RequestValidator.EnsureGuid(request.ProviderId, "providerId", "Provider ID");
        RequestValidator.EnsureFutureDateTime(
            request.AppointmentDateTime,
            currentTime,
            "appointmentDateTime");
    }

    private async Task<Appointment> GetRequiredAppointmentAsync(Guid appointmentId)
    {
        RequestValidator.EnsureGuid(appointmentId, nameof(appointmentId), "Appointment ID");

        return await appointmentRepository.GetAppointmentByIdAsync(appointmentId)
            ?? throw new ResourceNotFoundException("Appointment", appointmentId);
    }

    private void EnsureAppointmentWithinClinicHours(DateTimeOffset appointmentDateTime)
    {
        var appointmentEnd = appointmentDateTime.AddMinutes(clinicSettings.DefaultAppointmentDurationMinutes);
        var appointmentStartTime = TimeOnly.FromDateTime(appointmentDateTime.DateTime);
        var appointmentEndTime = TimeOnly.FromDateTime(appointmentEnd.DateTime);

        if (appointmentStartTime <= clinicSettings.DefaultClinicStartTime
            || appointmentEnd.Date != appointmentDateTime.Date
            || appointmentEndTime >= clinicSettings.DefaultClinicEndTime)
        {
            throw new AppointmentOutsideClinicHoursException(
                clinicSettings.DefaultClinicStartTime,
                clinicSettings.DefaultClinicEndTime);
        }
    }

    private async Task EnsureNoProviderAppointmentOverlapAsync(
        Guid providerId,
        DateTimeOffset requestedAppointmentDateTime,
        Guid? excludedAppointmentId)
    {
        var duration = TimeSpan.FromMinutes(clinicSettings.DefaultAppointmentDurationMinutes);
        var hasOverlap = await appointmentRepository.HasProviderAppointmentOverlapAsync(
            providerId,
            requestedAppointmentDateTime - duration,
            requestedAppointmentDateTime + duration,
            excludedAppointmentId);
        if (hasOverlap)
        {
            throw new ProviderAppointmentOverlapException(providerId);
        }
    }
}