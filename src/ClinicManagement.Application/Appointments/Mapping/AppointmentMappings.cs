using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Appointments.Mapping;

public static class AppointmentMappings
{
    public static Appointment ToEntity(this AppointmentRequest request, Patient patient, Provider provider) =>
        Appointment.Create(request.Id, patient, provider, request.AppointmentDateTime, request.ReasonForVisit);

    public static AppointmentResponse ToResponse(this Appointment appointment) =>
        new(
            appointment.Id,
            appointment.Patient.Id,
            appointment.Provider.Id,
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit);

    public static void ApplyTo(this AppointmentRequest request, Appointment appointment)
    {
        appointment.SetAppointmentDateTime(request.AppointmentDateTime);
        appointment.SetReasonForVisit(request.ReasonForVisit);
    }

    public static void ApplyTo(
        this AppointmentRequest request,
        Appointment appointment,
        Patient patient,
        Provider provider)
    {
        request.ApplyTo(appointment);
        appointment.SetPatient(patient);
        appointment.SetProvider(provider);
    }
}
