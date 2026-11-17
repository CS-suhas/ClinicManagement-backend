using ClinicManagement.Domain.Common;
using ClinicManagement.Domain.Enums;

namespace ClinicManagement.Domain.Entities;

public class Allergy : BaseEntity
{
    public Patient Patient { get; private set; } = null!;
    public AllergyType AllergyType { get; private set; } = null!;
    public AllergySeverity Severity { get; private set; }
    public string? Notes { get; private set; }

    private Allergy() { }

    public static Allergy Create(string name, AllergySeverity severity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Allergy name cannot be null or empty.", nameof(name));

        return new Allergy
        {
            AllergyType = AllergyType.Create(name),
            Severity = severity
        };
    }
}