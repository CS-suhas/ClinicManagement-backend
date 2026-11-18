using ClinicManagement.Application.Allergies.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Allergies.Mapping;

public static class AllergyMappings
{
    public static Allergy ToEntity(this AllergyRequest request, Patient patient, AllergyType allergyType) =>
        Allergy.Create(request.Id, patient, allergyType, request.Severity, request.Notes);

    public static AllergyResponse ToResponse(this Allergy allergy) =>
        new(
            allergy.Id,
            allergy.Patient.Id,
            allergy.AllergyType.Id,
            allergy.Severity,
            allergy.Notes);

    public static void ApplyTo(this AllergyRequest request, Allergy allergy)
    {
        allergy.SetSeverity(request.Severity);
        allergy.SetNotes(request.Notes);
    }

    public static void ApplyTo(
        this AllergyRequest request,
        Allergy allergy,
        Patient patient,
        AllergyType allergyType)
    {
        request.ApplyTo(allergy);
        allergy.SetPatient(patient);
        allergy.SetAllergyType(allergyType);
    }
}
