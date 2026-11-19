using ClinicManagement.Application.AllergyTypes.Models;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.AllergyTypes.Mapping;

public static class AllergyTypeMappings
{
    public static AllergyType ToEntity(this AllergyTypeRequest request) =>
        AllergyType.Create(request.Id, request.Name);

    public static AllergyTypeResponse ToResponse(this AllergyType allergyType) =>
        new(allergyType.Id, allergyType.Name);

    public static void ApplyTo(this AllergyTypeRequest request, AllergyType allergyType)
    {
        allergyType.SetName(request.Name);
    }
}
