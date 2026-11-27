using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Mapping;

public static class PatientAllergyMappings
{
    public static PatientAllergyResponse ToResponse(this PatientAllergy allergy) =>
        new(
            allergy.Id,
            allergy.AllergyId,
            allergy.AllergyType.Name,
            allergy.Severity,
            allergy.Notes);
}
