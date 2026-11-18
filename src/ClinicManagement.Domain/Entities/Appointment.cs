using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Entities;

public class Appointment : BaseEntity
{
    public Patient Patient { get; private set; } = null!;
    public Provider Provider { get; private set; } = null!;
    public DateTimeOffset AppointmentDateTime { get; private set; }
    public string? ReasonForVisit { get; private set; } = null;
    
    private Appointment() { }

    public static Appointment Create(Patient patient, Provider provider, DateTimeOffset dateTime, string? reasonForVisit)
    {
        if (patient == null)
            throw new ArgumentNullException(nameof(patient), "Patient cannot be null.");
        
        if (provider == null)
            throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");

        if (dateTime <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Appointment date and time must be in the future.", nameof(dateTime));

        
        return new Appointment
        {
            Patient = patient,
            Provider = provider,
            AppointmentDateTime = dateTime,
            ReasonForVisit = reasonForVisit
        };
    }

    public static Appointment Create(Guid id, Patient patient, Provider provider, DateTimeOffset dateTime, string? reasonForVisit)
    {
        var appointment = Create(patient, provider, dateTime, reasonForVisit);
        appointment.Id = id;
        return appointment;
    }

    public void SetAppointmentDateTime(DateTimeOffset dateTime)
    {
        if (dateTime <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Appointment date and time must be in the future.", nameof(dateTime));

        AppointmentDateTime = dateTime;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetReasonForVisit(string? reasonForVisit)
    {
        ReasonForVisit = reasonForVisit?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPatient(Patient patient)
    {
        Patient = patient ?? throw new ArgumentNullException(nameof(patient), "Patient cannot be null.");
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetProvider(Provider provider)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider), "Provider cannot be null.");
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static TimeSpan operator -(Appointment a1, Appointment a2)
    {
        return a1.AppointmentDateTime - a2.AppointmentDateTime;
    }
 
}
