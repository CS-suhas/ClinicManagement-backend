using ClinicManagement.Application.Clinics.Interfaces;
using ClinicManagement.Application.Common;
using ClinicManagement.Application.Common.Exceptions;
using ClinicManagement.Application.Appointments.Interfaces;
using ClinicManagement.Application.Appointments.Mapping;
using ClinicManagement.Application.Appointments.Models;
using ClinicManagement.Application.Clinics.Mapping;
using ClinicManagement.Application.Clinics.Models;
using ClinicManagement.Application.Providers.Interfaces;
using ClinicManagement.Application.Providers.Mapping;
using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;
using ClinicManagement.Application.Models;
using ClinicManagement.Application.Interfaces;

namespace ClinicManagement.Application.Providers.Services;

public class ProviderService(
    IProviderRepository providerRepository,
    IClinicRepository clinicRepository,
    IAppointmentRepository appointmentRepository,
    ICurrentUser currentUser) : IProviderService
{
    public async Task<ProviderResponse> GetProviderByIdAsync(Guid providerId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        var provider = await providerRepository.GetProviderByIdAsync(providerId)
            ?? throw new ResourceNotFoundException("Provider", providerId);
        return provider.ToResponse();
    }

    public async Task<List<ProviderResponse>> GetAllProvidersAsync()
    {
        var providers = await providerRepository.GetAllProvidersAsync();
        return providers.Select(item => item.ToResponse()).ToList();
    }

    public async Task<PagedResponse<ProviderResponse>> SearchProvidersAsync(
        PageQuery query,
        Guid? clinicId,
        ProviderVerificationStatus? verificationStatus = null)
    {
        var offset = query.GetOffset();
        var (items, totalCount) = await providerRepository.SearchProvidersAsync(
            query,
            offset,
            clinicId,
            verificationStatus);
        return ToPage(items, totalCount, query);
    }

    public async Task<PagedResponse<ProviderResponse>> SearchProviderDirectoryAsync(PageQuery query, Guid? clinicId)
    {
        var offset = query.GetOffset();
        var (items, totalCount) = await providerRepository.SearchPublicProvidersAsync(query, offset, clinicId);
        return ToPage(items, totalCount, query);
    }

    public async Task<Guid> RegisterProviderAsync(ProviderRegistrationRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Provider ID");
        RequestValidator.EnsureText(request.FirstName, "firstName", "First name");
        RequestValidator.EnsureText(request.LastName, "lastName", "Last name");
        RequestValidator.EnsureText(request.Specialty, "specialty", "Specialty");

        var subject = CurrentUserGuard.RequireSubject(currentUser);
        if (await providerRepository.GetByIdentitySubjectAsync(subject) is not null)
            throw new IdentityAlreadyLinkedException("provider", subject);

        var provider = request.ToEntity();
        provider.AssignIdentitySubject(subject);
        return await providerRepository.AddRegistrationAsync(provider);
    }

    public Task VerifyProviderAsync(Guid providerId) =>
        ChangeVerificationAsync(providerId, provider => provider.Verify());

    public Task RejectProviderAsync(Guid providerId) =>
        ChangeVerificationAsync(providerId, provider => provider.Reject());

    public async Task<List<AppointmentListResponse>> GetProviderCalendarAsync(
        Guid providerId,
        DateTimeOffset from,
        DateTimeOffset to,
        bool includeInactive)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");
        await CurrentUserGuard.RequireVerifiedProviderAsync(currentUser, providerId);
        if (from >= to)
            throw new DomainValidationException("'from' must be earlier than 'to'.", nameof(from));
        if (to - from > MaxCalendarRange)
            throw new DomainValidationException(
                $"The calendar range cannot exceed {MaxCalendarRange.TotalDays:0} days.",
                nameof(to));

        var appointments = await appointmentRepository.GetProviderAppointmentsInRangeAsync(
            providerId,
            from,
            to,
            includeInactive);
        return appointments.ToList();
    }

    public async Task<ClinicResponse> GetClinicByProviderIdAsync(Guid providerId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        var provider = await providerRepository.GetProviderByIdAsync(providerId)
            ?? throw new ResourceNotFoundException("Provider", providerId);
        if (provider.Clinic is null)
            throw new ResourceNotFoundException($"Provider '{providerId}' does not belong to a clinic.");
        return provider.Clinic.ToResponse();
    }

    public async Task<List<AppointmentResponse>> GetAppointmentsByProviderIdAsync(Guid providerId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        var provider = await providerRepository.GetProviderByIdAsync(providerId)
            ?? throw new ResourceNotFoundException("Provider", providerId);
        await EnsureCanViewProviderAppointmentsAsync(provider);

        var appointments = await appointmentRepository.GetAppointmentsByProviderIdAsync(providerId);
        return appointments.Select(appointment => appointment.ToResponse()).ToList();
    }

    public async Task<Guid> AddProviderAsync(ProviderRequest request)
    {
        ValidateRequest(request);

        var clinicExists = await clinicRepository.ClinicExistsAsync(request.ClinicId);
        if (!clinicExists)
        {
            throw new ResourceNotFoundException("Clinic", request.ClinicId);
        }
        var provider = request.ToEntity();
        await providerRepository.AddProviderAsync(provider, request.ClinicId);
        return provider.Id;
    }

    public async Task<Guid> UpdateProviderAsync(ProviderRequest request)
    {
        ValidateRequest(request);
        await EnsureCanUpdateProviderAsync(request);

        var provider = await providerRepository.GetProviderByIdAsync(request.Id);
        if (provider == null)
        {
            throw new ResourceNotFoundException("Provider", request.Id);
        }
        var clinic = await clinicRepository.ClinicExistsAsync(request.ClinicId);
        if (!clinic)
        {
            throw new ResourceNotFoundException("Clinic", request.ClinicId);
        }
        request.ApplyTo(provider);
        await providerRepository.UpdateProviderAsync(provider, request.ClinicId);
        return provider.Id;
    }

    public async Task<Guid> DeleteProviderAsync(Guid providerId)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        var provider = await providerRepository.GetProviderByIdAsync(providerId);
        if (provider == null)
        {
            throw new ResourceNotFoundException("Provider", providerId);
        }

        if (await providerRepository.HasAppointmentsAsync(providerId))
        {
            throw new ProviderHasAppointmentsException(providerId);
        }

        await providerRepository.DeleteProviderAsync(provider.Id);
        return providerId;
    }

    private static readonly TimeSpan MaxCalendarRange = TimeSpan.FromDays(62);

    private async Task ChangeVerificationAsync(Guid providerId, Action<Provider> change)
    {
        RequestValidator.EnsureGuid(providerId, nameof(providerId), "Provider ID");

        var provider = await providerRepository.GetProviderByIdAsync(providerId)
            ?? throw new ResourceNotFoundException("Provider", providerId);

        var expected = provider.VerificationStatus;
        change(provider);

        if (!await providerRepository.TryUpdateVerificationStatusAsync(providerId, expected, provider.VerificationStatus))
            throw new ConcurrentModificationException("Provider", providerId);
    }

    private static PagedResponse<ProviderResponse> ToPage(
        IReadOnlyList<Provider> items,
        int totalCount,
        PageQuery query) =>
        new(items.Select(item => item.ToResponse()).ToArray(), totalCount, query.Page, query.PageSize);

    private async Task EnsureCanUpdateProviderAsync(ProviderRequest request)
    {
        if (CurrentUserGuard.IsAdministrator(currentUser))
            return;

        var self = await CurrentUserGuard.RequireProviderAsync(currentUser, request.Id);
        if (self.ClinicId != request.ClinicId)
            throw new ForbiddenException("Providers cannot change their own clinic.");
    }

    private async Task EnsureCanViewProviderAppointmentsAsync(Provider provider)
    {
        CurrentUserGuard.RequireAuthenticated(currentUser);
        if (CurrentUserGuard.IsAdministrator(currentUser))
            return;
        if (await currentUser.GetProviderAsync() is { } self && self.Id == provider.Id)
            return;
        if (provider.ClinicId is { } clinicId && await currentUser.IsClinicAdminOfAsync(clinicId))
            return;

        throw new ForbiddenException($"Provider '{provider.Id}' appointments are not visible to the signed-in user.");
    }

    private static void ValidateRequest(ProviderRequest request)
    {
        RequestValidator.EnsureGuid(request.Id, "id", "Provider ID");
        RequestValidator.EnsureGuid(request.ClinicId, "clinicId", "Clinic ID");
        RequestValidator.EnsureText(request.FirstName, "firstName", "First name");
        RequestValidator.EnsureText(request.LastName, "lastName", "Last name");
        RequestValidator.EnsureText(request.Specialty, "specialty", "Specialty");
    }
}
