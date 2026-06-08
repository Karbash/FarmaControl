using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class CareAppointmentConfiguration : IEntityTypeConfiguration<CareAppointment>
{
    public void Configure(EntityTypeBuilder<CareAppointment> builder)
    {
        builder.ToTable("care_appointments");

        builder.HasKey(appointment => appointment.Id);

        builder.Property(appointment => appointment.Id).HasColumnName("id");
        builder.Property(appointment => appointment.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(appointment => appointment.Date).HasColumnName("date").IsRequired();
        builder.Property(appointment => appointment.Time).HasColumnName("time");
        builder.Property(appointment => appointment.Type).HasColumnName("type").HasMaxLength(80).IsRequired();
        builder.Property(appointment => appointment.IsEmergency).HasColumnName("is_emergency").IsRequired();
        builder.Property(appointment => appointment.Status)
            .HasColumnName("status")
            .HasConversion(status => status.Value, value => AppointmentStatus.From(value))
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(appointment => appointment.DoctorName).HasColumnName("doctor_name").HasMaxLength(160);
        builder.Property(appointment => appointment.Responsible).HasColumnName("responsible").HasMaxLength(160);
        builder.Property(appointment => appointment.Notes).HasColumnName("notes");
        builder.Property(appointment => appointment.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(appointment => appointment.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(appointment => appointment.PatientId);
        builder.HasIndex(appointment => appointment.Date);
        builder.HasIndex(appointment => appointment.Status);
    }
}
