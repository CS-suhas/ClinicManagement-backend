using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Exceptions;

namespace ClinicManagement.Domain.Entities;

public class AllergyType : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    private AllergyType() { }

    public static AllergyType Create(Guid id, string name)
    {
        var allergyType = Create(name);
        allergyType.Id = id;
        return allergyType;
    }

    public static AllergyType Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Allergy type name cannot be null or empty.", nameof(name));

        return new AllergyType
        {
            Name = name.Trim()
        };
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Allergy type name cannot be empty.", nameof(name));
        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}