using System.Globalization;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Application.Interfaces;
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
    ICurrentUser currentUser,
    ClinicManagementSettings clinicSettings,
    TimeProvider timeProvider) : IAppointmentService
{
    public async Task<Guid> AddAppointmentAsync(AppointmentRequest request)
    {
        var patient = await CurrentUserGuard.RequirePatientAsync(currentUser);
        request = request with { PatientId = patient.Id };
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
        var scoped = await ScopeSearchAsync(query);
        var offset = scoped.GetOffset();
        var (items, totalCount) = await appointmentRepository.SearchAppointmentsAsync(scoped, offset);
        return new PagedResponse<AppointmentListResponse>(items, totalCount, scoped.Page, scoped.PageSize);
    }

    public async Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId)
    {
        var appointment = await GetViewableAppointmentAsync(appointmentId);
        return appointment.ToResponse();
    }

    public async Task<ProviderBusySlotsResponse> GetProviderBusySlotsAsync(Guid providerId, string? date)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");
        var day = ParseBookingDate(date);

        // Unverified providers and providers without a clinic are not bookable, so they must not be discoverable.
        var provider = await providerRepository.GetProviderByIdAsync(providerId);
        if (provider is null || !provider.IsAcceptingAppointments)
            throw new ResourceNotFoundException("Provider", providerId);

        // "That date" is the UTC calendar day. See ParseBookingDate and the endpoint description.
        var dayStart = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var busyStartTimes = await appointmentRepository.GetActiveStartTimesInRangeAsync(
            providerId,
            dayStart,
            dayStart.AddDays(1));

        return new ProviderBusySlotsResponse(
            providerId,
            day,
            clinicSettings.DefaultAppointmentDurationMinutes,
            busyStartTimes);
    }

    private static DateOnly ParseBookingDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
            throw new DomainValidationException("Date is required in the format YYYY-MM-DD.", "date");
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            throw new DomainValidationException("Date must be a valid calendar date in the format YYYY-MM-DD.", "date");
        return day;
    }

    public async Task<PatientResponse> GetPatientByAppointmentIdAsync(Guid appointmentId) =>
        (await GetViewableAppointmentAsync(appointmentId)).Patient.ToResponse();

    public async Task<ProviderResponse> GetProviderByAppointmentIdAsync(Guid appointmentId) =>
        (await GetViewableAppointmentAsync(appointmentId)).Provider.ToResponse();

    public async Task<ClinicResponse> GetClinicByAppointmentIdAsync(Guid appointmentId)
    {
        var appointment = await GetViewableAppointmentAsync(appointmentId);
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
        ChangeProviderStatusAsync(appointmentId, (appointment, now) => appointment.Confirm(now));

    public Task DeclineAppointmentAsync(Guid appointmentId) =>
        ChangeProviderStatusAsync(appointmentId, (appointment, now) => appointment.Decline(now));

    public async Task CancelAppointmentAsync(Guid appointmentId)
    {
        var appointment = await GetRequiredAppointmentAsync(appointmentId);
        CurrentUserGuard.RequireAuthenticated(currentUser);
        if (!await IsParticipantAsync(appointment))
            throw new ForbiddenException("Only the appointment's patient or provider can cancel it.");
        await ApplyStatusChangeAsync(appointment, (item, now) => item.Cancel(now));
    }

    public Task CompleteAppointmentAsync(Guid appointmentId) =>
        ChangeProviderStatusAsync(appointmentId, (appointment, now) => appointment.Complete(now));

    private async Task ChangeProviderStatusAsync(Guid appointmentId, Action<Appointment, DateTimeOffset> transition)
    {
        var appointment = await GetRequiredAppointmentAsync(appointmentId);
        await CurrentUserGuard.RequireVerifiedProviderAsync(currentUser, appointment.ProviderId);
        await ApplyStatusChangeAsync(appointment, transition);
    }

    private async Task ApplyStatusChangeAsync(Appointment appointment, Action<Appointment, DateTimeOffset> transition)
    {
        var expected = appointment.Status;
        transition(appointment, timeProvider.GetUtcNow());

        if (!await appointmentRepository.TryUpdateStatusAsync(appointment.Id, expected, appointment.Status))
            throw new ConcurrentModificationException("Appointment", appointment.Id);
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

    private async Task<AppointmentSearchQuery> ScopeSearchAsync(AppointmentSearchQuery query)
    {
        CurrentUserGuard.RequireAuthenticated(currentUser);
        if (CurrentUserGuard.IsAdministrator(currentUser))
            return query;
        if (await currentUser.GetPatientAsync() is { } patient)
            return query with { PatientId = ScopeTo(query.PatientId, patient.Id) };
        if (query.ClinicId is { } clinicId && await currentUser.IsClinicAdminOfAsync(clinicId))
            return query;
        if (await currentUser.GetProviderAsync() is { } provider)
            return query with
            {
                ProviderId = ScopeTo(query.ProviderId, provider.Id),
                ProviderIds = ScopeToAll(query.ProviderIds, provider.Id)
            };

        throw new ForbiddenException("Appointment search requires a patient, a provider, or a clinic administrator.");
    }

    private static Guid ScopeTo(Guid? requested, Guid owned)
    {
        if (requested is { } value && value != owned)
            throw new ForbiddenException("Appointments of other patients or providers cannot be searched.");
        return owned;
    }

    private static Guid[]? ScopeToAll(Guid[]? requested, Guid owned)
    {
        if (requested is not null && requested.Any(value => value != owned))
            throw new ForbiddenException("Appointments of other patients or providers cannot be searched.");
        return requested;
    }

    private async Task<Appointment> GetViewableAppointmentAsync(Guid appointmentId)
    {
        var appointment = await GetRequiredAppointmentAsync(appointmentId);
        await EnsureCanViewAsync(appointment);
        return appointment;
    }

    private async Task EnsureCanViewAsync(Appointment appointment)
    {
        CurrentUserGuard.RequireAuthenticated(currentUser);
        if (CurrentUserGuard.IsAdministrator(currentUser) || await IsParticipantAsync(appointment))
            return;
        if (appointment.Provider.ClinicId is { } clinicId && await currentUser.IsClinicAdminOfAsync(clinicId))
            return;

        throw new ForbiddenException($"Appointment '{appointment.Id}' is not visible to the signed-in user.");
    }

    private async Task<bool> IsParticipantAsync(Appointment appointment)
    {
        if (await currentUser.GetPatientAsync() is { } patient && patient.Id == appointment.PatientId)
            return true;

        return await currentUser.GetProviderAsync() is { } provider && provider.Id == appointment.ProviderId;
    }

    private void EnsureAppointmentWithinClinicHours(DateTimeOffset appointmentDateTime)
    {
        var appointmentEnd = appointmentDateTime.AddMinutes(clinicSettings.DefaultAppointmentDurationMinutes);
        var appointmentStartTime = TimeOnly.FromDateTime(appointmentDateTime.DateTime);
        var appointmentEndTime = TimeOnly.FromDateTime(appointmentEnd.DateTime);

        // Opening and closing times are inclusive: a start at opening and an end at closing are both allowed.
        if (appointmentStartTime < clinicSettings.DefaultClinicStartTime
            || appointmentEnd.Date != appointmentDateTime.Date
            || appointmentEndTime > clinicSettings.DefaultClinicEndTime)
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