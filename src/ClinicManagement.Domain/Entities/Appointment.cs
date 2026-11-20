using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class Appointment : BaseEntity
{
    public Patient Patient { get; private set; } = null!;
    public Provider Provider { get; private set; } = null!;
    public Guid PatientId { get; private set; }
    public Guid ProviderId { get; private set; }
    public DateTimeOffset AppointmentDateTime { get; private set; }
    public string? ReasonForVisit { get; private set; } = null;
    
    private Appointment() { }

    public static Appointment Create(
        DateTimeOffset dateTime,
        string? reasonForVisit,
        Patient patient,
        Provider provider,
        DateTimeOffset currentTime)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(provider);

        var appointment = Create(
            Guid.NewGuid(),
            patient.Id,
            provider.Id,
            dateTime,
            reasonForVisit,
            currentTime);
        appointment.Patient = patient;
        appointment.Provider = provider;
        return appointment;
    }

    public static Appointment Create(
        Guid id,
        DateTimeOffset dateTime,
        string? reasonForVisit,
        Patient patient,
        Provider provider,
        DateTimeOffset currentTime)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(provider);

        var appointment = Create(
            id,
            patient.Id,
            provider.Id,
            dateTime,
            reasonForVisit,
            currentTime);
        appointment.Patient = patient;
        appointment.Provider = provider;
        return appointment;
    }

    public static Appointment Create(
        Guid id,
        Guid patientId,
        Guid providerId,
        DateTimeOffset dateTime,
        string? reasonForVisit,
        DateTimeOffset currentTime)
    {
        if (!DataValidator.IsValidGuid(id))
            throw new DomainValidationException("Appointment ID must be a non-empty GUID.", nameof(id));
        if (!DataValidator.IsValidGuid(patientId))
            throw new DomainValidationException("Patient ID must be a non-empty GUID.", nameof(patientId));
        if (!DataValidator.IsValidGuid(providerId))
            throw new DomainValidationException("Provider ID must be a non-empty GUID.", nameof(providerId));
        if (!DataValidator.IsFutureDateTime(dateTime, currentTime))
            throw new DomainValidationException("Appointment date and time must be in the future.", nameof(dateTime));

        return new Appointment
        {
            Id = id,
            PatientId = patientId,
            ProviderId = providerId,
            AppointmentDateTime = dateTime,
            ReasonForVisit = reasonForVisit
        };
    }

    public void SetAppointmentDateTime(DateTimeOffset dateTime, DateTimeOffset currentTime)
    {
        if (!DataValidator.IsFutureDateTime(dateTime, currentTime))
            throw new DomainValidationException("Appointment date and time must be in the future.", nameof(dateTime));

        AppointmentDateTime = dateTime;
        UpdatedAtUtc = currentTime.UtcDateTime;
    }

    public void SetReasonForVisit(string? reasonForVisit, DateTimeOffset currentTime)
    {
        ReasonForVisit = reasonForVisit?.Trim();
        UpdatedAtUtc = currentTime.UtcDateTime;
    }

    public void SetPatient(Patient patient)
    {
        Patient = patient ?? throw new ArgumentNullException(nameof(patient), "Patient cannot be null.");
        PatientId = patient.Id;
    }

    public void SetProvider(Provider provider)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");
        ProviderId = provider.Id;
    }

    public static TimeSpan operator -(Appointment a1, Appointment a2)
    {
        return a1.AppointmentDateTime - a2.AppointmentDateTime;
    }
 
}
