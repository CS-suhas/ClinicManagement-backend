using Microsoft.EntityFrameworkCore;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Patient> Patients { get; }
    DbSet<Clinic> Clinics { get; }
    DbSet<Provider> Providers { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Allergy> Allergies { get; }
    DbSet<AllergyType> AllergyTypes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}