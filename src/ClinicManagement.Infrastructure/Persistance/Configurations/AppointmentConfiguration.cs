using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ClinicManagement.Domain.Entities;

namespace ClinicManagement.Infrastructure.Persistance.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(appointment => appointment.Id);

        builder.Property(appointment => appointment.AppointmentDateTime)
            .IsRequired();

        builder.Property(appointment => appointment.ReasonForVisit)
            .HasMaxLength(500);

        builder.HasOne(appointment => appointment.Patient)
            .WithMany()
            .HasForeignKey("PatientId")
            .IsRequired();

        builder.HasOne(appointment => appointment.Provider)
            .WithMany()
            .HasForeignKey("ProviderId")
            .IsRequired();
    }
}
