namespace ClinicManagement.Application.Models;

public record AllergyTypeResponse(
    Guid Id,
    string Name,
    int PatientCount
);
