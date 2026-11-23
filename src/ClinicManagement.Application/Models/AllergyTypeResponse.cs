namespace ClinicManagement.Application.AllergyTypes.Models;

/// <summary>PatientCount is the number of patients who have recorded this allergy.</summary>
public record AllergyTypeResponse(
    Guid Id,
    string Name,
    int PatientCount
);
