using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Api.Tests.Support;

public abstract class ClinicApiTestBase(ClinicApiFactory factory)
{
    protected ClinicApiFactory Factory { get; } = factory;

    protected static readonly DateTimeOffset Booking = new(
        DateTime.UtcNow.Date.AddDays(3).AddHours(10), TimeSpan.Zero);

    protected const string CalendarQuery = "from=2030-01-01T00:00:00Z&to=2030-01-08T00:00:00Z";

    protected World SeedWorld()
    {
        var store = Factory.Store;

        var clinic = Clinic.Create(Guid.NewGuid(), "Riverside Clinic", "1 River Road", "555-0100");
        var otherClinic = Clinic.Create(Guid.NewGuid(), "Hillside Clinic", "2 Hill Road", "555-0101");
        store.Clinics.Add(clinic);
        store.Clinics.Add(otherClinic);

        var patientSubject = Subject("patient");
        var patient = Patient.Create(Guid.NewGuid(), "Ada", "Lovelace", new DateOnly(1985, 12, 10), "ada@example.com", "555-0102");
        patient.AssignIdentitySubject(patientSubject);
        store.Patients.Add(patient);

        var otherPatientSubject = Subject("patient");
        var otherPatient = Patient.Create(Guid.NewGuid(), "Grace", "Hopper", new DateOnly(1906, 12, 9), "grace@example.com", "555-0103");
        otherPatient.AssignIdentitySubject(otherPatientSubject);
        store.Patients.Add(otherPatient);

        var doctorSubject = Subject("doctor");
        var doctor = Provider.Create(Guid.NewGuid(), clinic.Id, "Alan", "Turing", "Computing");
        doctor.AssignIdentitySubject(doctorSubject);
        store.Providers.Add(doctor);

        var otherDoctorSubject = Subject("doctor");
        var otherDoctor = Provider.Create(Guid.NewGuid(), otherClinic.Id, "Katherine", "Johnson", "Mathematics");
        otherDoctor.AssignIdentitySubject(otherDoctorSubject);
        store.Providers.Add(otherDoctor);

        var pendingSubject = Subject("pending");
        var pending = Provider.Register(Guid.NewGuid(), "Edsger", "Dijkstra", "Algorithms");
        pending.AssignIdentitySubject(pendingSubject);
        store.Providers.Add(pending);

        var clinicAdminSubject = Subject("clinicadmin");
        store.ClinicAdmins.Add(ClinicAdmin.Create(Guid.NewGuid(), clinic.Id, clinicAdminSubject));

        var otherClinicAdminSubject = Subject("clinicadmin");
        store.ClinicAdmins.Add(ClinicAdmin.Create(Guid.NewGuid(), otherClinic.Id, otherClinicAdminSubject));

        var appointment = Appointment.Create(
            DateTimeOffset.UtcNow.AddDays(30),
            "Checkup",
            patient,
            doctor,
            DateTimeOffset.UtcNow);
        store.Appointments.Add(appointment);

        var joinRequest = ClinicJoinRequest.Create(Guid.NewGuid(), pending.Id, clinic.Id);
        store.JoinRequests.Add(joinRequest);

        return new World(
            patientSubject, patient,
            otherPatientSubject, otherPatient,
            doctorSubject, doctor,
            otherDoctorSubject, otherDoctor,
            pendingSubject, pending,
            clinicAdminSubject, clinic,
            otherClinicAdminSubject, otherClinic,
            appointment, joinRequest);
    }

    protected static string Subject(string role) => $"kc-{role}-{Guid.NewGuid():N}";

    protected async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string? subject,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (subject is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Factory.MintToken(subject));
        if (body is not null)
            request.Content = JsonContent.Create(body);

        return await Factory.CreateClient().SendAsync(request);
    }
}

public sealed record World(
    string PatientSubject,
    Patient Patient,
    string OtherPatientSubject,
    Patient OtherPatient,
    string DoctorSubject,
    Provider Doctor,
    string OtherDoctorSubject,
    Provider OtherDoctor,
    string PendingSubject,
    Provider PendingProvider,
    string ClinicAdminSubject,
    Clinic Clinic,
    string OtherClinicAdminSubject,
    Clinic OtherClinic,
    Appointment Appointment,
    ClinicJoinRequest JoinRequest)
{
    public string SubjectFor(string role) => role switch
    {
        "patient" => PatientSubject,
        "doctor" => DoctorSubject,
        "clinicadmin" => ClinicAdminSubject,
        "admin" => ClinicApiFactory.PlatformAdminSubject,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };
}
