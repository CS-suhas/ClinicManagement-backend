using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Entities;

public class AllergyType : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public ICollection<Allergy> Allergies { get; private set; } = new List<Allergy>();

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
            throw new ArgumentException("Allergy type name cannot be null or empty.", nameof(name));

        return new AllergyType
        {
            Name = name.Trim()
        };
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Allergy type name cannot be empty.", nameof(name));
        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}