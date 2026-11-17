using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Infrastructure.Persistance.Configurations;

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.HasKey(provider => provider.Id);

        builder.Property(provider => provider.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(provider => provider.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(provider => provider.Specialty)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(provider => provider.Clinic)
            .WithMany()
            .HasForeignKey("ClinicId")
            .IsRequired();

        builder.HasMany(provider => provider.Appointments)
            .WithOne()
            .HasForeignKey("AppointmentId");
    }
}
