using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class MedicalAttendanceCid10ItemConfiguration
    : IEntityTypeConfiguration<MedicalAttendanceCid10Item>
{
    public void Configure(EntityTypeBuilder<MedicalAttendanceCid10Item> builder)
    {
        builder.ToTable("medical_attendance_cid10_codes");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.MedicalAttendanceId)
            .HasColumnName("medical_attendance_id")
            .IsRequired();

        builder.Property(item => item.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(item => item.Cid10CodeId)
            .HasColumnName("cid10_code_id")
            .IsRequired();

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();

        builder.HasIndex(item => new { item.MedicalAttendanceId, item.Order });
        builder.HasIndex(item => item.Cid10CodeId);
        builder.HasIndex(item => new { item.MedicalAttendanceId, item.Cid10CodeId })
            .IsUnique();

        builder.HasOne<Cid10Code>()
            .WithMany()
            .HasForeignKey(item => item.Cid10CodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
