using Microsoft.EntityFrameworkCore;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Mapping;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Application.Common.Settings;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Appointments.Services;

public class AppointmentManagementService(IApplicationDbContext dbContext, ClinicManagementSettings clinicSettings) : IAppointmentManagementService
{
    public async Task<AppointmentResponse?> GetAppointmentByIdAsync(Guid appointmentId)
    {
        var appointment = await dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
            .Include(appointment => appointment.Provider)
            .FirstOrDefaultAsync(appointment => appointment.Id == appointmentId);

        return appointment?.ToResponse();
    }

    public async Task<List<AppointmentResponse>> GetAllAppointmentsAsync()
    {
        var appointments = await dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Patient)
            .Include(appointment => appointment.Provider)
            .ToListAsync();

        return appointments.Select(appointment => appointment.ToResponse()).ToList();
    }

    public async Task<Guid> AddAppointmentAsync(AppointmentRequest request)
    {
        var patient = await dbContext.Patients.FindAsync(request.PatientId)
            ?? throw new KeyNotFoundException($"Patient '{request.PatientId}' was not found.");
        var provider = await dbContext.Providers.FindAsync(request.ProviderId)
            ?? throw new KeyNotFoundException($"Provider '{request.ProviderId}' was not found.");

        var appointment = request.ToEntity(patient, provider);

        await EnsureNoProviderAppointmentOverlapAsync(appointment, clinicSettings.DefaultAppointmentDurationMinutes);

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();

        return appointment.Id;
    }

    public async Task<Guid> UpdateAppointmentAsync(AppointmentRequest request)
    {
        var appointment = await dbContext.Appointments.FindAsync(request.Id)
            ?? throw new KeyNotFoundException($"Appointment '{request.Id}' was not found.");
        var patient = await dbContext.Patients.FindAsync(request.PatientId)
            ?? throw new KeyNotFoundException($"Patient '{request.PatientId}' was not found.");
        var provider = await dbContext.Providers.FindAsync(request.ProviderId)
            ?? throw new KeyNotFoundException($"Provider '{request.ProviderId}' was not found.");

        request.ApplyTo(appointment, patient, provider);
        await EnsureNoProviderAppointmentOverlapAsync(appointment, clinicSettings.DefaultAppointmentDurationMinutes);
        await dbContext.SaveChangesAsync();

        return appointment.Id;
    }

    public async Task<Guid> DeleteAppointmentAsync(Guid appointmentId)
    {
        var appointment = await dbContext.Appointments.FindAsync(appointmentId)
            ?? throw new KeyNotFoundException($"Appointment '{appointmentId}' was not found.");

        dbContext.Appointments.Remove(appointment);
        await dbContext.SaveChangesAsync();

        return appointmentId;
    }

    private async Task EnsureNoProviderAppointmentOverlapAsync(
        Appointment appointment, Double appointmentDurationMinutes)
    {
        var overlapStart = appointment.AppointmentDateTime.AddMinutes(-appointmentDurationMinutes);
        var overlapEnd = appointment.AppointmentDateTime.AddMinutes(appointmentDurationMinutes);

        var hasOverlap = await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.Id != appointment.Id)
            .Where(a => a.Provider.Id == appointment.Provider.Id)
            .AnyAsync(a =>
                a.AppointmentDateTime > overlapStart &&
                a.AppointmentDateTime < overlapEnd);

        if (hasOverlap)
        {
            throw new ProviderAppointmentOverlapException(appointment.Provider.Id);
        }
    }
}
