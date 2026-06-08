using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("medical_records");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id");
        builder.Property(record => record.AppointmentId).HasColumnName("appointment_id").IsRequired();
        builder.Property(record => record.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(record => record.DoctorName).HasColumnName("doctor_name").HasMaxLength(160);
        builder.Property(record => record.Anamnesis).HasColumnName("anamnesis");
        builder.Property(record => record.PhysicalExam).HasColumnName("physical_exam");
        builder.Property(record => record.DiagnosticHypothesis).HasColumnName("diagnostic_hypothesis");
        builder.Property(record => record.Cid10).HasColumnName("cid10").HasMaxLength(40);
        builder.Property(record => record.Conduct).HasColumnName("conduct");
        builder.Property(record => record.Notes).HasColumnName("notes");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(record => record.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(record => record.AppointmentId);
        builder.HasIndex(record => record.PatientId);
    }
}
