using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Infrastructure.Persistance.Configurations;

public class AllergyTypeConfiguration : IEntityTypeConfiguration<AllergyType>
{
    public void Configure(EntityTypeBuilder<AllergyType> builder)
    {
        builder.HasKey(allergyType => allergyType.Id);

        builder.Property(allergyType => allergyType.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(allergyType => allergyType.Allergies)
            .WithOne(allergy => allergy.AllergyType)
            .HasForeignKey("AllergyTypeId")
            .IsRequired();
    }
}
