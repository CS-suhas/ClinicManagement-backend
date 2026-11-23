using ClinicManagement.Application.Providers.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Providers.Mapping;

public static class ProviderMappings
{
    public static Provider ToEntity(this ProviderRequest request)
    {
        var provider = Provider.Create(
            request.Id,
            request.ClinicId,
            request.FirstName,
            request.LastName,
            request.Specialty);
        provider.SetFee(request.Fee);
        provider.SetGender(ParseGender(request.Gender));
        return provider;
    }

    public static Provider ToEntity(this ProviderRegistrationRequest request)
    {
        var provider = Provider.Register(
            request.Id,
            request.FirstName,
            request.LastName,
            request.Specialty);
        provider.SetFee(request.Fee);
        provider.SetGender(ParseGender(request.Gender));
        return provider;
    }

    public static ProviderResponse ToResponse(this Provider provider) =>
        new(
            provider.Id,
            provider.ClinicId,
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            provider.VerificationStatus.ToString(),
            provider.Fee,
            provider.Gender?.ToString());

    public static void ApplyTo(this ProviderRequest request, Provider provider)
    {
        provider.SetFirstName(request.FirstName);
        provider.SetLastName(request.LastName);
        provider.SetSpecialty(request.Specialty);
        provider.SetFee(request.Fee);
        provider.SetGender(ParseGender(request.Gender));
    }

    /// <summary>Blank means "not specified". Anything else must name a <see cref="Gender"/> value.</summary>
    public static Gender? ParseGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return null;
        if (Enum.TryParse<Gender>(gender.Trim(), true, out var parsed) && Enum.IsDefined(parsed))
            return parsed;
        throw new DomainValidationException("Gender must be one of MALE, FEMALE or OTHER.", "gender");
    }
}
