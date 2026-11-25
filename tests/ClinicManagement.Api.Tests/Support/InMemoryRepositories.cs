using ClinicManagement.Application.Allergies.Interfaces;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Clinics.Interfaces;
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
    public List<ClinicAdmin> ClinicAdmins { get; } = [];
    public List<User> Users { get; } = [];
    public List<StoredUserRole> UserRoles { get; } = [];
    public List<AllergyType> AllergyTypes { get; } = [];
    /// <summary>When each appointment last changed status, mirroring Appointments.StatusChangedAtUtc.</summary>
    public Dictionary<Guid, DateTimeOffset> StatusChangedAt { get; } = [];

    /// <summary>The user for <paramref name="subject"/>, created when missing. Use it to seed linked rows.</summary>
    public User UserFor(string subject)
    {
        lock (Users)
        {
            var user = Users.FirstOrDefault(candidate => candidate.IdentitySubject == subject);
            if (user is null)
            {
                user = User.Create(Guid.NewGuid(), subject);
                Users.Add(user);
            }
            return user;
        }
    }
}

public sealed record StoredUserRole(Guid UserId, string Role, DateTimeOffset GrantedAtUtc, Guid? GrantedByUserId);

/// <summary>Allergy summary of a patient, computed the way the SQL summaries are.</summary>
internal static class InMemoryAllergySummary
{
    public static PatientAllergySummary From(Patient patient)
    {
        var highest = patient.Allergies.Count == 0
            ? (AllergySeverity?)null
            : patient.Allergies.Max(allergy => allergy.Severity);
        return new PatientAllergySummary(patient.Allergies.Count, highest?.ToString());
    }
}

/// <summary>Maps appointments to list items the same way the SQL list query does.</summary>
internal static class InMemoryAppointmentListItems
{
    public static AppointmentListResponse ToListItem(Appointment appointment, InMemoryStore store)
    {
        var allergies = appointment.Patient.Allergies;
        var highest = allergies.Count == 0 ? (AllergySeverity?)null : allergies.Max(allergy => allergy.Severity);
        var statusChangedAt = store.StatusChangedAt.TryGetValue(appointment.Id, out var changed)
            ? changed
            : appointment.CreatedAtUtc;

        return new AppointmentListResponse(
            appointment.Id,
            appointment.PatientId,
            $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
            appointment.ProviderId,
            $"{appointment.Provider.FirstName} {appointment.Provider.LastName}",
            appointment.Provider.Specialty,
            appointment.Provider.ClinicId,
            store.Clinics.FirstOrDefault(clinic => clinic.Id == appointment.Provider.ClinicId)?.Name ?? string.Empty,
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit,
            appointment.Status.ToString(),
            allergies.Count,
            highest?.ToString(),
            appointment.CreatedAtUtc,
            statusChangedAt);
    }
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

    public Task<Patient?> GetByUserIdAsync(Guid userId) =>
        Task.FromResult(store.Patients.FirstOrDefault(patient => patient.UserId == userId));

    public Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(PageQuery query, int offset)
    {
        var search = query.NormalizedSearch;
        var matches = store.Patients
            .Where(patient => search is null
                || patient.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || patient.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || $"{patient.FirstName} {patient.LastName}".Contains(search, StringComparison.OrdinalIgnoreCase)
                || patient.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                || patient.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(patient => patient.LastName).ThenBy(patient => patient.FirstName).ThenBy(patient => patient.Id)
            .ToList();
        IReadOnlyList<Patient> items = matches.Skip(offset).Take(query.PageSize).ToList();
        return Task.FromResult((items, matches.Count));
    }

    public Task<IReadOnlyDictionary<Guid, PatientAllergySummary>> GetAllergySummariesAsync(
        IReadOnlyCollection<Guid>? patientIds) =>
        Task.FromResult<IReadOnlyDictionary<Guid, PatientAllergySummary>>(store.Patients
            .Where(patient => patientIds is null || patientIds.Contains(patient.Id))
            .Where(patient => patient.Allergies.Count > 0)
            .ToDictionary(patient => patient.Id, InMemoryAllergySummary.From));

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
    private Provider Attach(Provider provider)
    {
        // The SQL repository joins the clinic onto every provider it returns; mirror that here.
        if (provider.Clinic is null && provider.ClinicId is { } clinicId
            && store.Clinics.FirstOrDefault(clinic => clinic.Id == clinicId) is { } clinic)
            provider.SetClinic(clinic);
        return provider;
    }

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
                new RosterPatient(
                    patient.Id,
                    patient.FirstName,
                    patient.LastName,
                    patient.DateOfBirth,
                    patient.Email,
                    patient.PhoneNumber,
                    patient.Allergies.Count,
                    InMemoryAllergySummary.From(patient).HighestSeverity),
                last is null ? null : new RosterVisit(last.Id, last.AppointmentDateTime, last.ReasonForVisit),
                next is null ? null : new RosterVisit(next.Id, next.AppointmentDateTime, next.ReasonForVisit),
                providerAppointments.Count(appointment => appointment.PatientId == patient.Id));
        }).ToList();
        return Task.FromResult<(IReadOnlyList<ProviderPatientRosterItem>, int)>((items, patients.Count));
    }

    public Task<IReadOnlyDictionary<Guid, int>> GetVerifiedProviderCountsByClinicAsync() =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(store.Providers
            .Where(provider => provider.VerificationStatus == ProviderVerificationStatus.VERIFIED)
            .GroupBy(provider => provider.ClinicId)
            .ToDictionary(group => group.Key, group => group.Count()));

    public Task<Provider?> GetProviderByIdAsync(Guid providerId) =>
        Task.FromResult<Provider?>(store.Providers.FirstOrDefault(provider => provider.Id == providerId) is { } found ? Attach(found) : null);

    public Task<IEnumerable<Provider>> GetAllProvidersAsync() =>
        Task.FromResult<IEnumerable<Provider>>(store.Providers.Select(Attach).ToList());

    public Task<IEnumerable<Provider>> GetProvidersByClinicIdAsync(Guid clinicId) =>
        Task.FromResult<IEnumerable<Provider>>(store.Providers.Where(provider => provider.ClinicId == clinicId).Select(Attach).ToList());

    public Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        store.Providers.RemoveAll(provider => provider.Id == providerId);
        return Task.FromResult(providerId);
    }

    public Task<bool> HasAppointmentsAsync(Guid providerId) =>
        Task.FromResult(store.Appointments.Any(appointment => appointment.ProviderId == providerId));

    public Task<bool> TryUpdateVerificationStatusAsync(
        Guid providerId,
        ProviderVerificationStatus expected,
        ProviderVerificationStatus next) =>
        Task.FromResult(store.Providers.Any(provider => provider.Id == providerId && provider.VerificationStatus == next));

    public Task<Provider?> GetByUserIdAsync(Guid userId) =>
        Task.FromResult<Provider?>(store.Providers.FirstOrDefault(provider => provider.UserId == userId) is { } found ? Attach(found) : null);

    public Task<(IReadOnlyList<Provider> Items, int TotalCount)> SearchPublicProvidersAsync(
        PageQuery query,
        int offset,
        Guid? clinicId)
    {
        var items = store.Providers
            .Where(provider => provider.VerificationStatus == ProviderVerificationStatus.VERIFIED
                && (clinicId is null || provider.ClinicId == clinicId))
            .ToList();
        items = items.Select(Attach).ToList();
        return Task.FromResult<(IReadOnlyList<Provider>, int)>((items, items.Count));
    }

    public Task<Guid> AddProviderAsync(Provider provider, Guid clinicId)
    {
        store.Providers.Add(provider);
        return Task.FromResult(provider.Id);
    }

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
        items = items.Select(Attach).ToList();
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

    public Task<(IReadOnlyList<Clinic> Items, int TotalCount)> SearchClinicsAsync(PageQuery query, int offset)
    {
        var search = query.NormalizedSearch;
        var matches = store.Clinics
            .Where(clinic => search is null
                || clinic.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (clinic.Address?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (clinic.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(clinic => clinic.Name, StringComparer.Ordinal)
            .ThenBy(clinic => clinic.Id)
            .ToList();
        IReadOnlyList<Clinic> items = matches.Skip(offset).Take(query.PageSize).ToList();
        return Task.FromResult<(IReadOnlyList<Clinic> Items, int TotalCount)>((items, matches.Count));
    }
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
        // The SQL repository hydrates patient and provider on read; attach them so listings can map the row.
        request.SetPatient(store.Patients.Single(patient => patient.Id == patientId));
        request.SetProvider(store.Providers.Single(provider => provider.Id == providerId));
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

    public Task<bool> TryUpdateStatusAsync(Guid appointmentId, AppointmentStatus expected, AppointmentStatus next)
    {
        var updated = store.Appointments.Any(appointment => appointment.Id == appointmentId && appointment.Status == next);
        if (updated)
            store.StatusChangedAt[appointmentId] = DateTimeOffset.UtcNow;
        return Task.FromResult(updated);
    }

    public Task<IReadOnlyList<AppointmentListResponse>> GetProviderAppointmentsInRangeAsync(
        Guid providerId,
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeInactive) =>
        Task.FromResult<IReadOnlyList<AppointmentListResponse>>(store.Appointments
            .Where(appointment => appointment.ProviderId == providerId
                && appointment.AppointmentDateTime >= start
                && appointment.AppointmentDateTime < end
                && (includeInactive
                    || (appointment.Status != AppointmentStatus.DECLINED && appointment.Status != AppointmentStatus.CANCELLED)))
            .OrderBy(appointment => appointment.AppointmentDateTime)
            .ThenBy(appointment => appointment.Id)
            .Select(appointment => InMemoryAppointmentListItems.ToListItem(appointment, store))
            .ToList());

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
            .Select(appointment => InMemoryAppointmentListItems.ToListItem(appointment, store))
            .ToList();
        return Task.FromResult<(IReadOnlyList<AppointmentListResponse>, int)>((items, ordered.Count));
    }

    public Task<IReadOnlyList<AppointmentListResponse>> GetUpcomingAppointmentsAsync(DateTimeOffset from, int count) =>
        throw new NotSupportedException();
}

internal sealed class InMemoryClinicAdminRepository(InMemoryStore store) : IClinicAdminRepository
{
    public Task<bool> IsAdminOfClinicAsync(Guid userId, Guid clinicId) =>
        Task.FromResult(store.ClinicAdmins.Any(admin => admin.UserId == userId && admin.ClinicId == clinicId));

    public Task<IReadOnlyList<Guid>> GetClinicIdsAsync(Guid userId) =>
        Task.FromResult<IReadOnlyList<Guid>>(store.ClinicAdmins
            .Where(admin => admin.UserId == userId)
            .Select(admin => admin.ClinicId)
            .OrderBy(id => id)
            .ToList());

    public Task AddAsync(ClinicAdmin clinicAdmin)
    {
        store.ClinicAdmins.Add(clinicAdmin);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryUserRepository(InMemoryStore store) : IUserRepository
{
    public Task<User?> GetBySubjectAsync(string identitySubject)
    {
        lock (store.Users)
            return Task.FromResult(store.Users.FirstOrDefault(user => user.IdentitySubject == identitySubject));
    }

    public Task<User> GetOrCreateAsync(string identitySubject, string? email = null, string? displayName = null)
    {
        var user = store.UserFor(identitySubject);
        user.UpdateProfile(email, displayName);
        return Task.FromResult(user);
    }

    public Task<UserAccessContext> GetAccessContextAsync(Guid userId)
    {
        var provider = store.Providers.FirstOrDefault(candidate => candidate.UserId == userId);
        return Task.FromResult(new UserAccessContext(
            userId,
            store.UserRoles.Where(role => role.UserId == userId).Select(role => role.Role).Order(StringComparer.Ordinal).ToList(),
            store.Patients.FirstOrDefault(patient => patient.UserId == userId)?.Id,
            provider?.Id,
            provider?.VerificationStatus,
            store.ClinicAdmins.Where(admin => admin.UserId == userId).Select(admin => admin.ClinicId).Distinct().ToList()));
    }
}

internal sealed class InMemoryUserRoleRepository(InMemoryStore store) : IUserRoleRepository
{
    public Task<bool> AssignAsync(Guid userId, string role, Guid? grantedByUserId)
    {
        lock (store.UserRoles)
        {
            if (store.UserRoles.Any(existing => existing.UserId == userId && existing.Role == role))
                return Task.FromResult(false);
            store.UserRoles.Add(new StoredUserRole(userId, role, DateTimeOffset.UtcNow, grantedByUserId));
            return Task.FromResult(true);
        }
    }

    public Task<bool> RevokeAsync(Guid userId, string role)
    {
        lock (store.UserRoles)
            return Task.FromResult(store.UserRoles.RemoveAll(existing => existing.UserId == userId && existing.Role == role) > 0);
    }

    public Task<IReadOnlyList<UserRoleAssignment>> ListAsync()
    {
        lock (store.UserRoles)
        {
            return Task.FromResult<IReadOnlyList<UserRoleAssignment>>(store.UserRoles
                .Select(role => (Role: role, User: store.Users.First(user => user.Id == role.UserId)))
                .OrderBy(pair => pair.Role.Role, StringComparer.Ordinal)
                .ThenBy(pair => pair.User.IdentitySubject, StringComparer.Ordinal)
                .Select(pair => new UserRoleAssignment(
                    pair.User.Id,
                    pair.User.IdentitySubject,
                    pair.User.Email,
                    pair.User.DisplayName,
                    pair.Role.Role,
                    pair.Role.GrantedAtUtc,
                    pair.Role.GrantedByUserId))
                .ToList());
        }
    }
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

    public Task<IReadOnlyDictionary<Guid, int>> GetPatientCountsByAllergyTypeAsync() =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(store.Patients
            .SelectMany(patient => patient.Allergies)
            .GroupBy(allergy => allergy.AllergyId)
            .ToDictionary(group => group.Key, group => group.Count()));

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
        var pendingRequests = own.Count(appointment => appointment.Status == AppointmentStatus.REQUESTED
            && appointment.AppointmentDateTime >= now);
        return Task.FromResult(new DoctorDashboardSection(
            CountToday(own, todayStart, todayEnd),
            week,
            own.Select(appointment => appointment.PatientId).Distinct().Count(),
            NextActive(own, now),
            pendingRequests));
    }

    public Task<PatientDashboardSection> GetPatientDashboardAsync(Guid patientId, DateTimeOffset now)
    {
        var own = store.Appointments.Where(appointment => appointment.PatientId == patientId).ToList();
        var upcoming = own.Count(appointment => IsActive(appointment) && appointment.AppointmentDateTime >= now);
        var recentDecisions = own
            .Where(appointment => appointment.Status is AppointmentStatus.CONFIRMED or AppointmentStatus.DECLINED)
            .Select(appointment => new PatientRecentDecision(
                appointment.Id,
                appointment.Status.ToString(),
                store.StatusChangedAt.TryGetValue(appointment.Id, out var changed) ? changed : appointment.CreatedAtUtc,
                $"{appointment.Provider.FirstName} {appointment.Provider.LastName}"))
            .OrderByDescending(decision => decision.DecidedAtUtc)
            .ThenBy(decision => decision.AppointmentId)
            .Take(5)
            .ToList();
        return Task.FromResult(new PatientDashboardSection(NextActive(own, now), upcoming, recentDecisions));
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
        return next is null ? null : InMemoryAppointmentListItems.ToListItem(next, store);
    }
}
