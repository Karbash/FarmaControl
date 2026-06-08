using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("prescriptions");

        builder.HasKey(prescription => prescription.Id);

        builder.Property(prescription => prescription.Id).HasColumnName("id");
        builder.Property(prescription => prescription.MedicalRecordId).HasColumnName("medical_record_id").IsRequired();
        builder.Property(prescription => prescription.PatientId).HasColumnName("patient_id").IsRequired();
        builder.Property(prescription => prescription.MedicationId).HasColumnName("medication_id");
        builder.Property(prescription => prescription.MedicationName).HasColumnName("medication_name").HasMaxLength(200);
        builder.Property(prescription => prescription.Dosage).HasColumnName("dosage").HasMaxLength(120);
        builder.Property(prescription => prescription.Directions).HasColumnName("directions");
        builder.Property(prescription => prescription.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(prescription => prescription.IsDispensed).HasColumnName("is_dispensed").IsRequired();
        builder.Property(prescription => prescription.Notes).HasColumnName("notes");
        builder.Property(prescription => prescription.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(prescription => prescription.DispensedAt).HasColumnName("dispensed_at");

        builder.HasIndex(prescription => prescription.MedicalRecordId);
        builder.HasIndex(prescription => prescription.PatientId);
        builder.HasIndex(prescription => prescription.IsDispensed);
    }
}
