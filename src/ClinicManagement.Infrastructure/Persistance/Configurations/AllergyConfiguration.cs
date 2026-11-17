using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Infrastructure.Persistance.Configurations;

public class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> builder)
    {
        builder.HasKey(allergy => allergy.Id);

        builder.Property(allergy => allergy.Severity)
            .IsRequired();

        builder.Property(allergy => allergy.Notes)
            .HasMaxLength(500);

        builder.HasOne(allergy => allergy.Patient)
            .WithMany()
            .HasForeignKey("PatientId");

        builder.HasOne(allergy => allergy.AllergyType)
            .WithMany()
            .HasForeignKey("AllergyTypeId");
    }
}
