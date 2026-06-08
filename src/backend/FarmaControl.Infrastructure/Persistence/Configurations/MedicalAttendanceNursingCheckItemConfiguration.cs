using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class MedicalAttendanceNursingCheckItemConfiguration
    : IEntityTypeConfiguration<MedicalAttendanceNursingCheckItem>
{
    public void Configure(EntityTypeBuilder<MedicalAttendanceNursingCheckItem> builder)
    {
        builder.ToTable("medical_attendance_nursing_checks");

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

        builder.HasIndex(item => new { item.MedicalAttendanceId, item.Order });
    }
}
