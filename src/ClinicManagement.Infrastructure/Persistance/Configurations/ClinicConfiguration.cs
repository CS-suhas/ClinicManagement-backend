using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Infrastructure.Persistance.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.HasKey(clinic => clinic.Id);

        builder.Property(clinic => clinic.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(clinic => clinic.Address)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(clinic => clinic.PhoneNumber)
            .IsRequired()
            .HasMaxLength(15);

        builder.HasMany(clinic => clinic.Providers)
            .WithOne(provider => provider.Clinic)
            .HasForeignKey("ClinicId")
            .IsRequired();
    }
}
