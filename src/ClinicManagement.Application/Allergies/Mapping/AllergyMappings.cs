using ClinicManagement.Application.Allergies.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Allergies.Mapping;

public static class AllergyMappings
{
    public static PatientAllergy ToEntity(this AllergyRequest request, Patient patient, AllergyType allergyType) =>
        PatientAllergy.Create(request.Id, patient, allergyType, request.Severity, request.Notes);

    public static AllergyResponse ToResponse(this PatientAllergy allergy) =>
        new(
            allergy.Id,
            allergy.Patient.Id,
            allergy.AllergyType.Id,
            allergy.Severity,
            allergy.Notes);

    public static void ApplyTo(this AllergyRequest request, PatientAllergy allergy)
    {
        allergy.SetSeverity(request.Severity);
        allergy.SetNotes(request.Notes);
    }

    public static void ApplyTo(
        this AllergyRequest request,
        PatientAllergy allergy,
        Patient patient,
        AllergyType allergyType)
    {
        request.ApplyTo(allergy);
        allergy.SetPatient(patient);
        allergy.SetAllergyType(allergyType);
    }
}
