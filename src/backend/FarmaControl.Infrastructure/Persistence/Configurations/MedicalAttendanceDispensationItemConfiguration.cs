using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class MedicalAttendanceDispensationItemConfiguration
    : IEntityTypeConfiguration<MedicalAttendanceDispensationItem>
{
    public void Configure(EntityTypeBuilder<MedicalAttendanceDispensationItem> builder)
    {
        builder.ToTable("medical_attendance_dispensations");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.MedicalAttendanceId)
            .HasColumnName("medical_attendance_id")
            .IsRequired();

        builder.Property(item => item.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(item => item.Batch)
            .HasColumnName("batch")
            .HasMaxLength(120);

        builder.Property(item => item.PrescriptionId)
            .HasColumnName("prescription_id");

        builder.Property(item => item.MedicationId)
            .HasColumnName("medication_id");

        builder.Property(item => item.MedicationName)
            .HasColumnName("medication_name")
            .HasMaxLength(200);

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity");

        builder.Property(item => item.Responsible)
            .HasColumnName("responsible")
            .HasMaxLength(160);

        builder.Property(item => item.DispensedAt)
            .HasColumnName("dispensed_at");

        builder.HasIndex(item => new { item.MedicalAttendanceId, item.Order });
        builder.HasIndex(item => item.PrescriptionId);
    }
}
