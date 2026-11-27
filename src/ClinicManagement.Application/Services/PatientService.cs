using ClinicManagement.Application.Interfaces;
using ClinicManagement.Application.Repositories;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Mapping;
using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Application.Services;

public class PatientService(
    IPatientRepository patientRepository,
    IAppointmentRepository appointmentRepository,
    IAllergyTypeRepository allergyTypeRepository,
    ICurrentUser currentUser,
    TimeProvider? timeProvider = null) : IPatientService
{
    private const int RecentVisitCount = 3;

    private TimeProvider Clock => timeProvider ?? TimeProvider.System;

    public async Task<PatientResponse> GetPatientByIdAsync(Guid patientId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        await currentUser.RequirePatientSelfOrAdministratorAsync(patientId);

        var patient = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);
        return patient.ToResponse();
    }

    public async Task<PatientSummaryResponse> GetPatientSummaryAsync(Guid patientId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        currentUser.RequireAuthenticated();

        var appointments = (await appointmentRepository.GetAppointmentsByPatientIdAsync(patientId)).ToList();
        var scope = await ResolvePatientSummaryScopeAsync(patientId, appointments);

        var patient = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);

        var visible = appointments.Where(scope.Includes).ToList();
        var now = Clock.GetUtcNow();
        var active = visible
            .Where(appointment => appointment.Status is not (AppointmentStatus.DECLINED or AppointmentStatus.CANCELLED))
            .ToList();

        var lastVisit = active
            .Where(appointment => appointment.AppointmentDateTime < now)
            .OrderByDescending(appointment => appointment.AppointmentDateTime)
            .ThenBy(appointment => appointment.Id)
            .FirstOrDefault();
        var nextVisit = active
            .Where(appointment => appointment.AppointmentDateTime >= now)
            .OrderBy(appointment => appointment.AppointmentDateTime)
            .ThenBy(appointment => appointment.Id)
            .FirstOrDefault();
        var recent = visible
            .OrderByDescending(appointment => appointment.AppointmentDateTime)
            .ThenBy(appointment => appointment.Id)
            .Take(RecentVisitCount)
            .Select(ToVisitResponse)
            .ToList();

        var allergies = (await patientRepository.GetAllergiesByPatientIdAsync(patientId))
            .Select(allergy => new PatientSummaryAllergy(
                allergy.AllergyId,
                allergy.AllergyType.Name,
                allergy.Severity))
            .ToList();

        return new PatientSummaryResponse(
            new PatientSummaryPatient(
                patient.Id,
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth,
                patient.Email,
                patient.PhoneNumber),
            allergies,
            lastVisit is null ? null : ToVisitResponse(lastVisit),
            nextVisit is null ? null : ToVisitResponse(nextVisit),
            new PatientVisitCounts(
                visible.Count,
                visible.Count(appointment => appointment.Status == AppointmentStatus.REQUESTED),
                visible.Count(appointment => appointment.Status == AppointmentStatus.CONFIRMED),
                visible.Count(appointment => appointment.Status == AppointmentStatus.COMPLETED),
                visible.Count(appointment => appointment.Status == AppointmentStatus.CANCELLED),
                visible.Count(appointment => appointment.Status == AppointmentStatus.DECLINED)),
            recent);
    }

    private async Task<PatientSummaryScope> ResolvePatientSummaryScopeAsync(
        Guid patientId,
        IReadOnlyCollection<Appointment> appointments)
    {
        if (currentUser.IsAdministrator())
            return PatientSummaryScope.All;

        if (await currentUser.GetPatientAsync() is { } self && self.Id == patientId)
            return PatientSummaryScope.All;

        var administeredClinicIds = (await currentUser.GetAdministeredClinicIdsAsync()).ToHashSet();
        if (administeredClinicIds.Count > 0
            && appointments.Any(appointment => administeredClinicIds.Contains(appointment.Provider.ClinicId)))
        {
            return PatientSummaryScope.ForClinics(administeredClinicIds);
        }

        if (await currentUser.GetProviderAsync() is { VerificationStatus: ProviderVerificationStatus.VERIFIED } doctor
            && appointments.Any(appointment => appointment.ProviderId == doctor.Id))
        {
            return PatientSummaryScope.ForProvider(doctor.Id);
        }

        throw new ForbiddenException($"Patient '{patientId}' summary is not visible to the signed-in user.");
    }

    private static PatientVisitResponse ToVisitResponse(Appointment appointment) =>
        new(
            appointment.Id,
            appointment.AppointmentDateTime,
            appointment.ReasonForVisit,
            appointment.Status,
            $"{appointment.Provider.FirstName} {appointment.Provider.LastName}");

    private sealed class PatientSummaryScope
    {
        public static readonly PatientSummaryScope All = new(_ => true);

        private readonly Func<Appointment, bool> _includes;

        private PatientSummaryScope(Func<Appointment, bool> includes) => _includes = includes;

        public static PatientSummaryScope ForClinics(IReadOnlySet<Guid> clinicIds) =>
            new(appointment => clinicIds.Contains(appointment.Provider.ClinicId));

        public static PatientSummaryScope ForProvider(Guid providerId) =>
            new(appointment => appointment.ProviderId == providerId);

        public bool Includes(Appointment appointment) => _includes(appointment);
    }

    public async Task<List<PatientListItemResponse>> GetAllPatientsAsync()
    {
        var patients = (await patientRepository.GetAllPatientsAsync()).ToList();
        var summaries = await patientRepository.GetAllergySummariesAsync(null);
        return patients.Select(patient => patient.ToListItem(summaries)).ToList();
    }

    public async Task<PagedResponse<PatientListItemResponse>> SearchPatientsAsync(PageQuery query)
    {
        var offset = query.GetOffset();
        var (items, totalCount) = await patientRepository.SearchPatientsAsync(query, offset);
        var summaries = await patientRepository.GetAllergySummariesAsync(items.Select(item => item.Id).ToArray());
        return new PagedResponse<PatientListItemResponse>(
            items.Select(item => item.ToListItem(summaries)).ToArray(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<IEnumerable<PatientAllergyResponse>> GetPatientAllergiesAsync(Guid patientId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        await currentUser.RequirePatientSelfOrAdministratorAsync(patientId);

        _ = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);

        var allergies = await patientRepository.GetAllergiesByPatientIdAsync(patientId);
        return allergies.Select(allergy => allergy.ToResponse());
    }

    public async Task<PagedResponse<PatientAllergyResponse>> SearchPatientAllergiesAsync(
        Guid patientId,
        PageQuery query)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        await currentUser.RequirePatientSelfOrAdministratorAsync(patientId);
        _ = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);

        var offset = query.GetOffset();
        var (items, totalCount) = await patientRepository.SearchPatientAllergiesAsync(patientId, query, offset);
        return new PagedResponse<PatientAllergyResponse>(
            items.Select(item => item.ToResponse()).ToArray(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<PagedResponse<AllergyTypeResponse>> GetSelectableAllergyTypesAsync(
        Guid patientId,
        Guid? selectedAllergyId,
        PageQuery query)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        await currentUser.RequirePatientSelfOrAdministratorAsync(patientId);
        _ = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);
        if (selectedAllergyId.HasValue)
            RequestValidator.EnsureGuid(selectedAllergyId.Value, nameof(selectedAllergyId), "Allergy type ID");

        var offset = query.GetOffset();
        var (items, totalCount) = await patientRepository.GetSelectableAllergyTypesAsync(
            patientId,
            selectedAllergyId,
            query,
            offset);
        var counts = await allergyTypeRepository.GetPatientCountsByAllergyTypeAsync();
        return new PagedResponse<AllergyTypeResponse>(
            items.Select(item => item.ToResponse(counts.GetValueOrDefault(item.Id))).ToArray(),
            totalCount,
            query.Page,
            query.PageSize);
    }

    public async Task<Guid> UpsertPatientAllergyAsync(Guid patientId, PatientAllergyRequest request)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        RequestValidator.EnsureGuid(request.AllergyId, nameof(request.AllergyId), "Allergy type ID");
        await currentUser.RequirePatientSelfAsync(patientId);

        _ = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);
        _ = await allergyTypeRepository.GetAllergyTypeByIdAsync(request.AllergyId)
            ?? throw new ResourceNotFoundException("Allergy type", request.AllergyId);

        var severity = request.Severity;
        if (!Enum.IsDefined(severity))
        {
            throw new DomainValidationException("Allergy severity is invalid.", nameof(request.Severity));
        }

        return await patientRepository.UpsertPatientAllergyAsync(
            patientId,
            request.AllergyId,
            severity,
            request.Notes);
    }

    public async Task<Guid> DeletePatientAllergyAsync(Guid patientId, Guid allergyId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        RequestValidator.EnsureGuid(allergyId, nameof(allergyId), "Allergy type ID");
        await currentUser.RequirePatientSelfAsync(patientId);

        _ = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);
        if (await patientRepository.DeletePatientAllergyAsync(patientId, allergyId) == Guid.Empty)
            throw new ResourceNotFoundException("Patient allergy", allergyId);
        return allergyId;
    }

    public async Task<List<AppointmentResponse>> GetAppointmentsByPatientIdAsync(Guid patientId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");
        await currentUser.RequirePatientSelfOrAdministratorAsync(patientId);

        _ = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);

        var appointments = await appointmentRepository.GetAppointmentsByPatientIdAsync(patientId);
        return appointments.Select(appointment => appointment.ToResponse()).ToList();
    }

    public async Task<Guid> AddPatientAsync(PatientRequest patient)
    {
        ValidateRequest(patient);

        var newPatient = patient.ToEntity();

        await patientRepository.AddPatientAsync(newPatient);
        return newPatient.Id;
    }

    public async Task<Guid> UpdatePatientAsync(PatientRequest request)
    {
        ValidateRequest(request);
        await currentUser.RequirePatientSelfAsync(request.Id);

        var patient = await patientRepository.GetPatientByIdAsync(request.Id)
            ?? throw new ResourceNotFoundException("Patient", request.Id);

        request.ApplyTo(patient);
        await patientRepository.UpdatePatientAsync(patient);
        return patient.Id;
    }

    public async Task<Guid> DeletePatientAsync(Guid patientId)
    {
        RequestValidator.EnsureGuid(patientId, nameof(patientId), "Patient ID");

        var patient = await patientRepository.GetPatientByIdAsync(patientId)
            ?? throw new ResourceNotFoundException("Patient", patientId);

        if (await patientRepository.HasDependentRecordsAsync(patientId))
        {
            throw new PatientHasRecordsException(patientId);
        }

        await patientRepository.DeletePatientAsync(patient.Id);
        return patientId;
    }

    private static void ValidateRequest(PatientRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Patient ID");
        RequestValidator.EnsureText(request.FirstName, "firstName", "First name");
        RequestValidator.EnsureText(request.LastName, "lastName", "Last name");
        RequestValidator.EnsureDateOfBirth(request.DateOfBirth);
        RequestValidator.EnsureText(request.PhoneNumber, "phoneNumber", "Phone number");
        RequestValidator.EnsureEmail(request.Email);
    }
}