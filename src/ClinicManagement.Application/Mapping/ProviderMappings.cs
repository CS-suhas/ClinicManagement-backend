using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;
using ClinicManagement.Domain.Enums;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Application.Mapping;

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
        provider.SetGender(EnsureGender(request.Gender));
        return provider;
    }

    public static Provider ToEntity(this ProviderRegistrationRequest request)
    {
        var provider = Provider.Register(
            request.Id,
            request.ClinicId,
            request.FirstName,
            request.LastName,
            request.Specialty);
        provider.SetFee(request.Fee);
        provider.SetGender(EnsureGender(request.Gender));
        return provider;
    }

    public static ProviderResponse ToResponse(this Provider provider) =>
        new(
            provider.Id,
            provider.ClinicId,
            provider.FirstName,
            provider.LastName,
            provider.Specialty,
            provider.VerificationStatus,
            provider.Fee,
            provider.Gender,
            provider.Clinic?.Name);

    public static void ApplyTo(this ProviderRequest request, Provider provider)
    {
        provider.SetFirstName(request.FirstName);
        provider.SetLastName(request.LastName);
        provider.SetSpecialty(request.Specialty);
        provider.SetFee(request.Fee);
        provider.SetGender(EnsureGender(request.Gender));
    }

    public static Gender? EnsureGender(Gender? gender)
    {
        if (gender is null || Enum.IsDefined(gender.Value))
            return gender;
        throw new DomainValidationException("Gender must be one of MALE, FEMALE or OTHER.", "gender");
    }
}
