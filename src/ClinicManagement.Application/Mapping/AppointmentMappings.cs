using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Appointments.Mapping;

public static class AppointmentMappings
{
    public static Appointment ToEntity(this AppointmentRequest request, DateTimeOffset currentTime) =>
        Appointment.Create(
            request.Id,
            request.PatientId,
            request.ProviderId,
            request.AppointmentDateTime,
            request.ReasonForVisit,
            currentTime);

    public static AppointmentResponse ToResponse(this Appointment appointment) =>
        new(
            appointment.Id,
            appointment.Patient.Id,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
            appointment.Provider.Id,
            $"{appointment.Provider.FirstName} {appointment.Provider.LastName}",
            appointment.Provider.Specialty,
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit,
            appointment.Status.ToString());

    public static void ApplyTo(this AppointmentRequest request, Appointment appointment, DateTimeOffset currentTime)
    {
        appointment.SetAppointmentDateTime(request.AppointmentDateTime, currentTime);
        appointment.SetReasonForVisit(request.ReasonForVisit, currentTime);
    }
}
