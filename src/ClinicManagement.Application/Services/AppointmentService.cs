using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Appointments.Mapping;

namespace ClinicManagement.Application.Appointments.Services;

public class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    ClinicManagementSettings clinicSettings,
    TimeProvider timeProvider) : IAppointmentService
{
    public async Task<Guid> AddAppointmentAsync(AppointmentRequest request)
    {
        var patientExists = await patientRepository.PatientExistsAsync(request.PatientId);

        if (!patientExists)
        {
            throw new ResourceNotFoundException("Patient", request.PatientId);
        }

        var providerAppointment = await appointmentRepository.GetAppointmentsByProviderIdAsync(request.ProviderId);

        foreach (var existingAppointment in providerAppointment)
        {
            EnsureNoProviderAppointmentOverlap(
                request.ProviderId,
                request.AppointmentDateTime,
                existingAppointment,
                clinicSettings.DefaultAppointmentDurationMinutes);
        }

        var newAppointment = request.ToEntity(timeProvider.GetUtcNow());
        await appointmentRepository.AddAppointmentAsync(newAppointment, request.PatientId, request.ProviderId);
        return request.Id;
    }

    public async Task<Guid> DeleteAppointmentAsync(Guid appointmentId)
    {
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

    public async Task<AppointmentResponse> GetAppointmentByIdAsync(Guid appointmentId)
    {
        var appointment = await appointmentRepository.GetAppointmentByIdAsync(appointmentId)
                                ?? throw new ResourceNotFoundException("Appointment", appointmentId);
        return appointment.ToResponse();
    }

    public async Task<Guid> UpdateAppointmentAsync(AppointmentRequest request)
    {
        var existingAppointment = await appointmentRepository.GetAppointmentByIdAsync(request.Id)
            ?? throw new ResourceNotFoundException("Appointment", request.Id);

        if (existingAppointment.AppointmentDateTime != request.AppointmentDateTime
            || existingAppointment.ProviderId != request.ProviderId)
        {
            var providerAppointments = await appointmentRepository.GetAppointmentsByProviderIdAsync(request.ProviderId);

            foreach (var appointment in providerAppointments)
            {
                if (appointment.Id != request.Id)
                {
                    EnsureNoProviderAppointmentOverlap(
                        request.ProviderId,
                        request.AppointmentDateTime,
                        appointment,
                        clinicSettings.DefaultAppointmentDurationMinutes);
                }
            }
        }
        return await appointmentRepository.UpdateAppointmentAsync(
            request.ToEntity(timeProvider.GetUtcNow()),
            request.PatientId,
            request.ProviderId);
    }

    private static void EnsureNoProviderAppointmentOverlap(
        Guid providerId,
        DateTimeOffset requestedAppointmentDateTime,
        Appointment existingAppointment,
        double appointmentDurationMinutes)
    {
        var timeDifference = Math.Abs(
            (existingAppointment.AppointmentDateTime - requestedAppointmentDateTime).TotalMinutes);

        if (timeDifference < appointmentDurationMinutes)
        {
            throw new ProviderAppointmentOverlapException(providerId);
        }
    }
}