using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Entities;

public class AllergyType : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public ICollection<Allergy> Allergies { get; private set; } = new List<Allergy>();

    private AllergyType() { }

    public static AllergyType Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Allergy type name cannot be null or empty.", nameof(name));

        return new AllergyType
        {
            Name = name.Trim()
        };
    }
}