using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Infrastructure.Persistance.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {

        builder.HasKey(patient => patient.Id);

        builder.Property(patient => patient.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(patient => patient.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(patient => patient.DateOfBirth)
            .IsRequired();

        builder.Property(patient => patient.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(patient => patient.PhoneNumber)
            .IsRequired()
            .HasMaxLength(15);

        builder.HasMany(patient => patient.Allergies)
            .WithOne()
            .HasForeignKey("PatientId")
            .IsRequired();

        builder.HasMany(patient => patient.Appointments)
            .WithOne()
            .HasForeignKey("PatientId")
            .IsRequired();
    }
}