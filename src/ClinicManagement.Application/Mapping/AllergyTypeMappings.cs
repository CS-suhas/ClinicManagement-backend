using ClinicManagement.Application.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Mapping;

public static class AllergyTypeMappings
{
    public static AllergyType ToEntity(this AllergyTypeRequest request) =>
        AllergyType.Create(request.Id, request.Name);

    public static AllergyTypeResponse ToResponse(this AllergyType allergyType, int patientCount) =>
        new(allergyType.Id, allergyType.Name, patientCount);

    public static void ApplyTo(this AllergyTypeRequest request, AllergyType allergyType)
    {
        allergyType.SetName(request.Name);
    }
}
