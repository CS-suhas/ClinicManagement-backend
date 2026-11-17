using ClinicManagement.Domain.Common;

namespace ClinicManagement.Domain.Entities;

public class Provider : BaseEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Specialty { get; private set; } = string.Empty;
    public Clinic Clinic { get; private set; } = null!;
    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();
}