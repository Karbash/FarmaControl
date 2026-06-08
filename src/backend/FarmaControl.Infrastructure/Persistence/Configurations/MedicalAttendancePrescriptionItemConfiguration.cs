using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class MedicalAttendancePrescriptionItemConfiguration
    : IEntityTypeConfiguration<MedicalAttendancePrescriptionItem>
{
    public void Configure(EntityTypeBuilder<MedicalAttendancePrescriptionItem> builder)
    {
        builder.ToTable("medical_attendance_prescriptions");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.MedicalAttendanceId)
            .HasColumnName("medical_attendance_id")
            .IsRequired();

        builder.Property(item => item.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(item => item.Description)
            .HasColumnName("description");

        builder.Property(item => item.MedicationId)
            .HasColumnName("medication_id");

        builder.Property(item => item.MedicationName)
            .HasColumnName("medication_name");

        builder.Property(item => item.Dosage)
            .HasColumnName("dosage");

        builder.Property(item => item.Directions)
            .HasColumnName("directions");

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity");

        builder.HasIndex(item => new { item.MedicalAttendanceId, item.Order });
    }
}
