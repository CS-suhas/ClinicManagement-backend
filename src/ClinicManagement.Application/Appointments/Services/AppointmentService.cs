using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Appointments.Mapping;

namespace ClinicManagement.Application.Appointments.Services;

public class AppointmentService(IAppointmentRepository appointmentRepository, IPatientRepository patientRepository, ClinicManagementSettings clinicSettings) : IAppointmentService
{
    public async Task<Guid> AddAppointmentAsync(AppointmentRequest request)
    {
        var patientExists = await patientRepository.PatientExistsAsync(request.PatientId);

        if (!patientExists)
        {
            throw new KeyNotFoundException($"Patient '{request.PatientId}' was not found.");
        }

        var providerAppointment = await appointmentRepository.GetAppointmentsByProviderIdAsync(request.ProviderId);

        var newAppointment = request.ToEntity();

        foreach (var existingAppointment in providerAppointment)
        {
            await EnsureNoProviderAppointmentOverlapAsync(existingAppointment, newAppointment, clinicSettings.DefaultAppointmentDurationMinutes);
        }

        await appointmentRepository.AddAppointmentAsync(newAppointment, request.PatientId, request.ProviderId);
        return request.Id;
    }

    public async Task<Guid> DeleteAppointmentAsync(Guid appointmentId)
    {
        return await appointmentRepository.DeleteAppointmentAsync(appointmentId);
    }

    public async Task<List<AppointmentResponse>> GetAllAppointmentsAsync()
    {
        return (await appointmentRepository.GetAllAppointmentsAsync())
            .Select(appointment => appointment.ToResponse())
            .ToList();
    }

    public async Task<AppointmentResponse?> GetAppointmentByIdAsync(Guid appointmentId)
    {
        var appointment = await appointmentRepository.GetAppointmentByIdAsync(appointmentId)
                                ?? throw new KeyNotFoundException($"Appointment '{appointmentId}' was not found.");
        return appointment.ToResponse();
    }

    public async Task<Guid> UpdateAppointmentAsync(AppointmentRequest request)
    {
        var existingAppointment = await appointmentRepository.GetAppointmentByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Appointment '{request.Id}' was not found.");

        if (existingAppointment.AppointmentDateTime != request.AppointmentDateTime)
        {
            var providerAppointments = await appointmentRepository.GetAppointmentsByProviderIdAsync(request.ProviderId);

            foreach (var appointment in providerAppointments)
            {
                if (appointment.Id != request.Id)
                {
                    await EnsureNoProviderAppointmentOverlapAsync(existingAppointment, appointment, clinicSettings.DefaultAppointmentDurationMinutes);
                }
            }
        }
        return await appointmentRepository.UpdateAppointmentAsync(request.ToEntity(), request.PatientId, request.ProviderId);
    }

    private async Task EnsureNoProviderAppointmentOverlapAsync(
        Appointment appointment1, Appointment appointment2, Double appointmentDurationMinutes)
    {
        var timediff = Math.Abs((appointment2 - appointment1).TotalMinutes);

        if (timediff < appointmentDurationMinutes)
        {
            throw new ProviderAppointmentOverlapException(appointment1.Provider.Id);
        }
    }
}