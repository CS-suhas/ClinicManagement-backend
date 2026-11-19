using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Appointments.Mapping;

public static class AppointmentMappings
{
    public static Appointment ToEntity(this AppointmentRequest request) =>
        Appointment.Create(request.Id, request.AppointmentDateTime, request.ReasonForVisit);

    public static AppointmentResponse ToResponse(this Appointment appointment) =>
        new(
            appointment.Id,
            appointment.Patient.Id,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
            appointment.Provider.Id,
            $"{appointment.Provider.FirstName} {appointment.Provider.LastName}",
            appointment.Provider.Specialty,
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit);

    public static void ApplyTo(this AppointmentRequest request, Appointment appointment)
    {
        appointment.SetAppointmentDateTime(request.AppointmentDateTime);
        appointment.SetReasonForVisit(request.ReasonForVisit);
    }
}
