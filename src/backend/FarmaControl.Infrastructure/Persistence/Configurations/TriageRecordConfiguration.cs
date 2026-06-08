using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class TriageRecordConfiguration : IEntityTypeConfiguration<TriageRecord>
{
    public void Configure(EntityTypeBuilder<TriageRecord> builder)
    {
        builder.ToTable("triage_records");
        builder.HasKey(triage => triage.Id);
        builder.Property(triage => triage.Id).HasColumnName("id");
        builder.Property(triage => triage.AppointmentId).HasColumnName("appointment_id").IsRequired();
        builder.Property(triage => triage.BloodPressure).HasColumnName("blood_pressure").HasMaxLength(80);
        builder.Property(triage => triage.Temperature).HasColumnName("temperature").HasMaxLength(80);
        builder.Property(triage => triage.Weight).HasColumnName("weight").HasMaxLength(80);
        builder.Property(triage => triage.Height).HasColumnName("height").HasMaxLength(80);
        builder.Property(triage => triage.HeartRate).HasColumnName("heart_rate").HasMaxLength(80);
        builder.Property(triage => triage.OxygenSaturation).HasColumnName("oxygen_saturation").HasMaxLength(80);
        builder.Property(triage => triage.ChiefComplaint).HasColumnName("chief_complaint");
        builder.Property(triage => triage.Responsible).HasColumnName("responsible").HasMaxLength(160);
        builder.Property(triage => triage.Notes).HasColumnName("notes");
        builder.Property(triage => triage.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(triage => triage.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(triage => triage.AppointmentId);
    }
}
