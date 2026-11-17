using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ClinicManagement.Application.Common.Interfaces;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Application.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {}

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<AllergyType> AllergyTypes => Set<AllergyType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
