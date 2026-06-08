using FarmaControl.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Id).HasColumnName("id");
        builder.Property(movement => movement.Type).HasColumnName("type").HasMaxLength(80).IsRequired();
        builder.Property(movement => movement.MedicationId).HasColumnName("medication_id").IsRequired();
        builder.Property(movement => movement.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(movement => movement.Date).HasColumnName("date").IsRequired();
        builder.Property(movement => movement.Responsible).HasColumnName("responsible").HasMaxLength(160).IsRequired();
        builder.Property(movement => movement.Notes).HasColumnName("notes");
        builder.Property(movement => movement.Batch).HasColumnName("batch").HasMaxLength(120);
        builder.Property(movement => movement.Reason).HasColumnName("reason").HasMaxLength(160);
        builder.Property(movement => movement.AttendanceId).HasColumnName("attendance_id");
        builder.Property(movement => movement.AppointmentId).HasColumnName("appointment_id");
        builder.Property(movement => movement.PrescriptionId).HasColumnName("prescription_id");
        builder.Property(movement => movement.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(movement => movement.MedicationId);
        builder.HasIndex(movement => movement.Date);
        builder.HasIndex(movement => movement.Type);
        builder.HasIndex(movement => movement.AttendanceId);
        builder.HasIndex(movement => movement.AppointmentId);
        builder.HasIndex(movement => movement.PrescriptionId);
    }
}
