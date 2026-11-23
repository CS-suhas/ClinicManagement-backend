using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.ClinicJoinRequests.Interfaces;
using ClinicManagement.Application.ClinicJoinRequests.Models;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Patients.Interfaces;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Api.Tests.Support;

public sealed class InMemoryStore
{
    public List<Patient> Patients { get; } = [];
    public List<Provider> Providers { get; } = [];
    public List<Clinic> Clinics { get; } = [];
    public List<Appointment> Appointments { get; } = [];
    public List<ClinicJoinRequest> JoinRequests { get; } = [];
    public List<ClinicAdmin> ClinicAdmins { get; } = [];
    public List<string> PlatformAdminSubjects { get; } = [];
    public List<AllergyType> AllergyTypes { get; } = [];
}

internal sealed class InMemoryPatientRepository(InMemoryStore store) : IPatientRepository
{
    public Task<Patient?> GetPatientByIdAsync(Guid patientId) =>
        Task.FromResult(store.Patients.FirstOrDefault(patient => patient.Id == patientId));

    public Task<IEnumerable<Patient>> GetAllPatientsAsync() =>
        Task.FromResult<IEnumerable<Patient>>(store.Patients.ToList());

    public Task<Guid> AddPatientAsync(Patient patient)
    {
        store.Patients.Add(patient);
        return Task.FromResult(patient.Id);
    }

    public Task<Guid> UpdatePatientAsync(Patient patient) => Task.FromResult(patient.Id);

    public Task<Guid> DeletePatientAsync(Guid patientId)
    {
        store.Patients.RemoveAll(patient => patient.Id == patientId);
        return Task.FromResult(patientId);
    }

    public Task<bool> PatientExistsAsync(Guid patientId) =>
        Task.FromResult(store.Patients.Any(patient => patient.Id == patientId));

    public Task<bool> HasDependentRecordsAsync(Guid patientId) =>
        Task.FromResult(store.Appointments.Any(appointment => appointment.PatientId == patientId));

    public Task<IEnumerable<PatientAllergy>> GetAllergiesByPatientIdAsync(Guid patientId) =>
        Task.FromResult<IEnumerable<PatientAllergy>>(
            store.Patients.FirstOrDefault(patient => patient.Id == patientId)?.Allergies.ToList() ?? []);

    public Task<Patient?> GetByIdentitySubjectAsync(string identitySubject) =>
        Task.FromResult(store.Patients.FirstOrDefault(patient => patient.IdentitySubject == identitySubject));

    public Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(PageQuery query, int offset) =>
        throw new NotSupportedException();

    public Task<(IReadOnlyList<PatientAllergy> Items, int TotalCount)> SearchPatientAllergiesAsync(
        Guid patientId,
        PageQuery query,
        int offset) => throw new NotSupportedException();

    public Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> GetSelectableAllergyTypesAsync(
        Guid patientId,
        Guid? selectedAllergyId,
        PageQuery query,
        int offset) => throw new NotSupportedException();

    public Task<Guid> UpsertPatientAllergyAsync(
        Guid patientId,
        Guid allergyId,
        AllergySeverity severity,
        string? notes) => throw new NotSupportedException();

    public Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId) => throw new NotSupportedException();
}

internal sealed class InMemoryProviderRepository(InMemoryStore store) : IProviderRepository
{
    public Task<(IReadOnlyList<ProviderPatientRosterItem> Items, int TotalCount)> SearchPatientRosterAsync(
        Guid providerId,
        PageQuery query,
        int offset,
        DateTimeOffset now)
    {
        var providerAppointments = store.Appointments.Where(appointment => appointment.ProviderId == providerId).ToList();
        var patients = store.Patients
            .Where(patient => providerAppointments.Any(appointment => appointment.PatientId == patient.Id))
            .Where(patient => query.NormalizedSearch is not { } search
                || patient.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || patient.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || $"{patient.FirstName} {patient.LastName}".Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(patient => patient.LastName).ThenBy(patient => patient.FirstName).ThenBy(patient => patient.Id)
            .ToList();

        var items = patients.Skip(offset).Take(query.PageSize).Select(patient =>
        {
            var visits = providerAppointments
                .Where(appointment => appointment.PatientId == patient.Id
                    && appointment.Status is not (AppointmentStatus.DECLINED or AppointmentStatus.CANCELLED))
                .ToList();
            var last = visits.Where(visit => visit.AppointmentDateTime < now)
                .OrderByDescending(visit => visit.AppointmentDateTime).FirstOrDefault();
            var next = visits.Where(visit => visit.AppointmentDateTime >= now)
                .OrderBy(visit => visit.AppointmentDateTime).FirstOrDefault();
            return new ProviderPatientRosterItem(
                new RosterPatient(patient.Id, patient.FirstName, patient.LastName, patient.DateOfBirth, patient.Email, patient.PhoneNumber),
                last is null ? null : new RosterVisit(last.Id, last.AppointmentDateTime, last.ReasonForVisit),
                next is null ? null : new RosterVisit(next.Id, next.AppointmentDateTime, next.ReasonForVisit),
                providerAppointments.Count(appointment => appointment.PatientId == patient.Id));
        }).ToList();
        return Task.FromResult<(IReadOnlyList<ProviderPatientRosterItem>, int)>((items, patients.Count));
    }

    public Task<Provider?> GetProviderByIdAsync(Guid providerId) =>
        Task.FromResult(store.Providers.FirstOrDefault(provider => provider.Id == providerId));

    public Task<IEnumerable<Provider>> GetAllProvidersAsync() =>
        Task.FromResult<IEnumerable<Provider>>(store.Providers.ToList());

    public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
        Task.FromResult<IEnumerable<Provider>>(store.Providers.Where(provider => provider.ClinicId == clinicId).ToList());

    public Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        store.Providers.RemoveAll(provider => provider.Id == providerId);
        return Task.FromResult(providerId);
    }

    public Task<bool> HasAppointmentsAsync(Guid providerId) =>
        Task.FromResult(store.Appointments.Any(appointment => appointment.ProviderId == providerId));

    public Task<Guid> AddRegistrationAsync(Provider provider)
    {
        store.Providers.Add(provider);
        return Task.FromResult(provider.Id);
    }

    public Task<bool> TryUpdateVerificationStatusAsync(
        Guid providerId,
        ProviderVerificationStatus expected,
        ProviderVerificationStatus next) =>
        Task.FromResult(store.Providers.Any(provider => provider.Id == providerId && provider.VerificationStatus == next));

    public Task<Provider?> GetByIdentitySubjectAsync(string identitySubject) =>
        Task.FromResult(store.Providers.FirstOrDefault(provider => provider.IdentitySubject == identitySubject));

    public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId)
    {
        var items = store.Providers
            .Where(provider => provider.VerificationStatus == ProviderVerificationStatus.VERIFIED
                && provider.ClinicId is not null
                && (clinicId is null || provider.ClinicId == clinicId))
            .ToList();
        return Task.FromResult<(IReadOnlyList<Provider>, int)>((items, items.Count));
    }

    public Task<Guid> AddProviderAsync(Provider request, Guid clinicId) => throw new NotSupportedException();

    public Task<Guid> UpdateProviderAsync(Provider request, Guid clinicId) => throw new NotSupportedException();

    public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus = null)
    {
        var items = store.Providers
            .Where(provider => (clinicId is null || provider.ClinicId == clinicId)
                && (verificationStatus is null || provider.VerificationStatus == verificationStatus))
            .ToList();
        return Task.FromResult<(IReadOnlyList<Provider>, int)>((items, items.Count));
    }
}

internal sealed class InMemoryClinicRepository(InMemoryStore store) : IClinicRepository
{
    public Task<Clinic?> GetClinicByIdAsync(Guid clinicId) =>
        Task.FromResult(store.Clinics.FirstOrDefault(clinic => clinic.Id == clinicId));

    public Task<IEnumerable<Clinic>> GetAllClinicsAsync() =>
        Task.FromResult<IEnumerable<Clinic>>(store.Clinics.ToList());

    public Task<Guid> AddClinicAsync(Clinic request)
    {
        store.Clinics.Add(request);
        return Task.FromResult(request.Id);
    }

    public Task<Guid> UpdateClinicAsync(Clinic request) => Task.FromResult(request.Id);

    public Task<Guid> DeleteClinicAsync(Guid clinicId)
    {
        store.Clinics.RemoveAll(clinic => clinic.Id == clinicId);
        return Task.FromResult(clinicId);
    }

    public Task<bool> ClinicExistsAsync(Guid clinicId) =>
        Task.FromResult(store.Clinics.Any(clinic => clinic.Id == clinicId));

    public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(PageQuery query, int offset) =>
        throw new NotSupportedException();
}

internal sealed class InMemoryAppointmentRepository(InMemoryStore store) : IAppointmentRepository
{
    public Task<IReadOnlyList<DateTimeOffset>> GetActiveStartTimesInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end) =>
        Task.FromResult<IReadOnlyList<DateTimeOffset>>(store.Appointments
            .Where(appointment => appointment.ProviderId == providerId
                && appointment.AppointmentDateTime >= start
                && appointment.AppointmentDateTime < end
                && (appointment.Status == AppointmentStatus.REQUESTED || appointment.Status == AppointmentStatus.CONFIRMED))
            .Select(appointment => appointment.AppointmentDateTime)
            .OrderBy(time => time)
            .ToList());

    public Task<Appointment?> GetAppointmentByIdAsync(Guid appointmentId) =>
        Task.FromResult(store.Appointments.FirstOrDefault(appointment => appointment.Id == appointmentId));

    public Task<IEnumerable<Appointment>> GetAllAppointmentsAsync() =>
        Task.FromResult<IEnumerable<Appointment>>(store.Appointments.ToList());

    public Task<Guid> AddAppointmentAsync(Appointment request, Guid patientId, Guid providerId)
    {
        store.Appointments.Add(request);
        return Task.FromResult(request.Id);
    }

    public Task<IEnumerable<Appointment>> GetAppointmentsByProviderIdAsync(Guid providerId) =>
        Task.FromResult<IEnumerable<Appointment>>(
            store.Appointments.Where(appointment => appointment.ProviderId == providerId).ToList());

    public Task<IEnumerable<Appointment>> GetAppointmentsByPatientIdAsync(Guid patientId) =>
        Task.FromResult<IEnumerable<Appointment>>(
            store.Appointments.Where(appointment => appointment.PatientId == patientId).ToList());

    public Task<IEnumerable<Appointment>> GetAppointmentsByClinicIdAsync(Guid clinicId) =>
        Task.FromResult<IEnumerable<Appointment>>(
            store.Appointments.Where(appointment => appointment.Provider?.ClinicId == clinicId).ToList());

    public Task<bool> TryUpdateStatusAsync(Guid appointmentId, AppointmentStatus expected, AppointmentStatus next) =>
        Task.FromResult(store.Appointments.Any(appointment => appointment.Id == appointmentId && appointment.Status == next));

    public Task<IReadOnlyList<AppointmentListResponse>> GetProviderAppointmentsInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeInactive) =>
        Task.FromResult<IReadOnlyList<AppointmentListResponse>>([]);

    public Task<bool> HasProviderAppointmentOverlapAsync(
        Guid providerId,
        DateTimeOffset appointmentStart,
        DateTimeOffset appointmentEnd,
        Guid? excludedAppointmentId) =>
        Task.FromResult(store.Appointments.Any(appointment =>
            appointment.ProviderId == providerId
            && appointment.Id != excludedAppointmentId
            && appointment.AppointmentDateTime > appointmentStart
            && appointment.AppointmentDateTime < appointmentEnd));

    public Task<Guid> UpdateAppointmentAsync(Appointment request, Guid patientId, Guid providerId) =>
        throw new NotSupportedException();

    public Task<Guid> DeleteAppointmentAsync(Guid appointmentId) => throw new NotSupportedException();

    public Task<(IReadOnlyList<AppointmentListResponse> Items, int TotalCount)> SearchAppointmentsAsync(
        AppointmentSearchQuery query,
        int offset)
    {
        IEnumerable<Appointment> matches = store.Appointments;
        if (query.From is { } from)
            matches = matches.Where(appointment => appointment.AppointmentDateTime >= from);
        if (query.To is { } to)
            matches = matches.Where(appointment => appointment.AppointmentDateTime <= to);
        if (query.ClinicId is { } clinicId)
            matches = matches.Where(appointment => appointment.Provider.ClinicId == clinicId);
        if (query.ProviderId is { } providerId)
            matches = matches.Where(appointment => appointment.ProviderId == providerId);
        if (query.ProviderIds is { Length: > 0 } providerIds)
            matches = matches.Where(appointment => providerIds.Contains(appointment.ProviderId));
        if (query.PatientId is { } patientId)
            matches = matches.Where(appointment => appointment.PatientId == patientId);
        if (query.Status is { } status)
            matches = matches.Where(appointment => appointment.Status == status);

        var ordered = matches.OrderBy(appointment => appointment.AppointmentDateTime).ThenBy(appointment => appointment.Id).ToList();
        var items = ordered
            .Skip(offset)
            .Take(query.PageSize)
            .Select(appointment => new AppointmentListResponse(
                appointment.Id,
                appointment.PatientId,
                $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
                appointment.ProviderId,
                $"{appointment.Provider.FirstName} {appointment.Provider.LastName}",
                appointment.Provider.Specialty,
                appointment.Provider.ClinicId ?? Guid.Empty,
                store.Clinics.FirstOrDefault(clinic => clinic.Id == appointment.Provider.ClinicId)?.Name ?? string.Empty,
                appointment.AppointmentDateTime,
                appointment.ReasonForVisit,
                appointment.Status.ToString()))
            .ToList();
        return Task.FromResult<(IReadOnlyList<AppointmentListResponse>, int)>((items, ordered.Count));
    }

    public Task<IReadOnlyList<AppointmentListResponse>> GetUpcomingAppointmentsAsync(DateTimeOffset from, int count) =>
        throw new NotSupportedException();
}

internal sealed class InMemoryClinicJoinRequestRepository(InMemoryStore store) : IClinicJoinRequestRepository
{
    public Task<ClinicJoinRequest?> GetByIdAsync(Guid requestId) =>
        Task.FromResult(store.JoinRequests.FirstOrDefault(request => request.Id == requestId));

    public Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByProviderIdAsync(Guid providerId) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ClinicJoinRequestResponse>> GetByClinicIdAsync(
        Guid clinicId,
        ClinicJoinRequestStatus? status) => throw new NotSupportedException();

    public Task<bool> HasPendingAsync(Guid providerId) =>
        Task.FromResult(store.JoinRequests.Any(request =>
            request.ProviderId == providerId && request.Status == ClinicJoinRequestStatus.PENDING));

    public Task<Guid> AddAsync(ClinicJoinRequest request)
    {
        store.JoinRequests.Add(request);
        return Task.FromResult(request.Id);
    }

    public Task<bool> TryApproveAsync(ClinicJoinRequest approvedRequest) =>
        Task.FromResult(approvedRequest.Status == ClinicJoinRequestStatus.APPROVED);

    public Task<bool> TryDecideAsync(ClinicJoinRequest decidedRequest) => Task.FromResult(true);
}

internal sealed class InMemoryClinicAdminRepository(InMemoryStore store) : IClinicAdminRepository
{
    public Task<bool> IsAdminOfClinicAsync(string identitySubject, Guid clinicId) =>
        Task.FromResult(store.ClinicAdmins.Any(admin =>
            admin.IdentitySubject == identitySubject && admin.ClinicId == clinicId));

    public Task<bool> IsAnyAdminAsync(string identitySubject) =>
        Task.FromResult(store.ClinicAdmins.Any(admin => admin.IdentitySubject == identitySubject));

    public Task<IReadOnlyList<Guid>> GetClinicIdsAsync(string identitySubject) =>
        Task.FromResult<IReadOnlyList<Guid>>(store.ClinicAdmins
            .Where(admin => admin.IdentitySubject == identitySubject)
            .Select(admin => admin.ClinicId)
            .OrderBy(id => id)
            .ToList());

    public Task AddAsync(ClinicAdmin clinicAdmin)
    {
        store.ClinicAdmins.Add(clinicAdmin);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryPlatformAdminRepository(InMemoryStore store) : IPlatformAdminRepository
{
    public Task<bool> IsAdminAsync(string identitySubject) =>
        Task.FromResult(store.PlatformAdminSubjects.Contains(identitySubject));
}

internal sealed class InMemoryAllergyTypeRepository(InMemoryStore store) : IAllergyTypeRepository
{
    public Task<AllergyType?> GetAllergyTypeByIdAsync(Guid allergyTypeId) =>
        Task.FromResult(store.AllergyTypes.FirstOrDefault(allergyType => allergyType.Id == allergyTypeId));

    public Task<IEnumerable<AllergyType>> GetAllAllergyTypesAsync() =>
        Task.FromResult<IEnumerable<AllergyType>>(store.AllergyTypes.ToList());

    public Task<IEnumerable<Patient>> GetPatientsByAllergyTypeIdAsync(Guid allergyTypeId) =>
        Task.FromResult<IEnumerable<Patient>>([]);

    public Task<bool> HasPatientsWithAllergyTypeAsync(Guid allergyTypeId) => Task.FromResult(false);

    public Task<Guid> AddAllergyTypeAsync(AllergyType request)
    {
        store.AllergyTypes.Add(request);
        return Task.FromResult(request.Id);
    }

    public Task<Guid> UpdateAllergyTypeAsync(AllergyType request) => Task.FromResult(request.Id);

    public Task<Guid> DeleteAllergyTypeAsync(Guid allergyTypeId)
    {
        store.AllergyTypes.RemoveAll(allergyType => allergyType.Id == allergyTypeId);
        return Task.FromResult(allergyTypeId);
    }

    public Task<(IReadOnlyList<AllergyType> Items, int TotalCount)> SearchAllergyTypesAsync(PageQuery query, int offset)
    {
        var items = store.AllergyTypes.ToList();
        return Task.FromResult<(IReadOnlyList<AllergyType>, int)>((items, items.Count));
    }
}

internal sealed class InMemoryStatusRepository(InMemoryStore store) : IStatusRepository
{
    public Task<StatusStatistics> GetStatisticsAsync() =>
        Task.FromResult(new StatusStatistics(
            store.Clinics.Count,
            store.Providers.Count,
            store.Patients.Count,
            store.Appointments.Count));

    public Task<DashboardResponse> GetDashboardAsync(DateTimeOffset now) =>
        Task.FromResult(new DashboardResponse(
            store.Clinics.Count,
            store.Providers.Count,
            store.Patients.Count,
            store.AllergyTypes.Count,
            store.Appointments.Count,
            []));

    public Task<DashboardScopeCounts> GetPlatformCountsAsync(DateTimeOffset todayStart, DateTimeOffset todayEnd) =>
        Task.FromResult(new DashboardScopeCounts(
            store.Clinics.Count,
            store.Providers.Count,
            store.Patients.Count,
            store.Providers.Count(provider => provider.VerificationStatus == ProviderVerificationStatus.PENDING),
            CountToday(store.Appointments, todayStart, todayEnd)));

    public Task<DashboardScopeCounts> GetClinicCountsAsync(
        IReadOnlyCollection<Guid> clinicIds,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd)
    {
        var providers = store.Providers
            .Where(provider => provider.ClinicId is { } clinicId && clinicIds.Contains(clinicId))
            .ToList();
        var providerIds = providers.Select(provider => provider.Id).ToHashSet();
        var appointments = store.Appointments.Where(appointment => providerIds.Contains(appointment.ProviderId)).ToList();
        return Task.FromResult(new DashboardScopeCounts(
            clinicIds.Distinct().Count(),
            providers.Count,
            appointments.Select(appointment => appointment.PatientId).Distinct().Count(),
            providers.Count(provider => provider.VerificationStatus == ProviderVerificationStatus.PENDING),
            CountToday(appointments, todayStart, todayEnd)));
    }

    public Task<int> CountPendingJoinRequestsAsync(IReadOnlyCollection<Guid> clinicIds) =>
        Task.FromResult(store.JoinRequests.Count(request =>
            clinicIds.Contains(request.ClinicId) && request.Status == ClinicJoinRequestStatus.PENDING));

    public Task<DoctorDashboardSection> GetDoctorDashboardAsync(
        Guid providerId,
        DateTimeOffset now,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd,
        DateTimeOffset weekEnd)
    {
        var own = store.Appointments.Where(appointment => appointment.ProviderId == providerId).ToList();
        var week = own.Count(appointment => IsCounted(appointment)
            && appointment.AppointmentDateTime >= todayStart
            && appointment.AppointmentDateTime < weekEnd);
        return Task.FromResult(new DoctorDashboardSection(
            CountToday(own, todayStart, todayEnd),
            week,
            own.Select(appointment => appointment.PatientId).Distinct().Count(),
            NextActive(own, now)));
    }

    public Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now)
    {
        var own = store.Appointments.Where(appointment => appointment.PatientId == patientId).ToList();
        var upcoming = own.Count(appointment => IsActive(appointment) && appointment.AppointmentDateTime >= now);
        return Task.FromResult(new PatientDashboardSection(NextActive(own, now), upcoming));
    }

    private static bool IsCounted(Appointment appointment) =>
        appointment.Status is not (AppointmentStatus.DECLINED or AppointmentStatus.CANCELLED);

    private static bool IsActive(Appointment appointment) =>
        appointment.Status is AppointmentStatus.REQUESTED or AppointmentStatus.CONFIRMED;

    private static int CountToday(IEnumerable<Appointment> appointments, DateTimeOffset todayStart, DateTimeOffset todayEnd) =>
        appointments.Count(appointment => IsCounted(appointment)
            && appointment.AppointmentDateTime >= todayStart
            && appointment.AppointmentDateTime < todayEnd);

    private AppointmentListResponse? NextActive(IEnumerable<Appointment> appointments, DateTimeOffset now)
    {
        var next = appointments
            .Where(appointment => IsActive(appointment) && appointment.AppointmentDateTime >= now)
            .OrderBy(appointment => appointment.AppointmentDateTime)
            .ThenBy(appointment => appointment.Id)
            .FirstOrDefault();
        return next is null ? null : new AppointmentListResponse(
            next.Id,
            next.PatientId,
            $"{next.Patient.FirstName} {next.Patient.LastName}",
            next.ProviderId,
            $"{next.Provider.FirstName} {next.Provider.LastName}",
            next.Provider.Specialty,
            next.Provider.ClinicId ?? Guid.Empty,
            store.Clinics.FirstOrDefault(clinic => clinic.Id == next.Provider.ClinicId)?.Name ?? string.Empty,
            next.AppointmentDateTime,
            next.ReasonForVisit,
            next.Status.ToString());
    }
}
