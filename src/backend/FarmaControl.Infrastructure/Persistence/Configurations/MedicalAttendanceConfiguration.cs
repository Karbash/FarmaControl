using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class MedicalAttendanceConfiguration : IEntityTypeConfiguration<MedicalAttendance>
{
    public void Configure(EntityTypeBuilder<MedicalAttendance> builder)
    {
        builder.ToTable("medical_attendances");

        builder.HasKey(attendance => attendance.Id);

        builder.Property(attendance => attendance.Id)
            .HasColumnName("id");

        builder.Property(attendance => attendance.AppointmentId)
            .HasColumnName("appointment_id")
            .IsRequired();

        builder.Property(attendance => attendance.PatientId)
            .HasColumnName("patient_id")
            .IsRequired();

        builder.Property(attendance => attendance.ResponsibleUserId)
            .HasColumnName("responsible_user_id");

        builder.Property(attendance => attendance.ResponsibleName)
            .HasColumnName("responsible_name")
            .HasMaxLength(160);

        builder.Property(attendance => attendance.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(attendance => attendance.Age)
            .HasColumnName("age");

        builder.Property(attendance => attendance.AttendanceDate)
            .HasColumnName("attendance_date")
            .IsRequired();

        builder.Property(attendance => attendance.AttendanceTime)
            .HasColumnName("attendance_time");

        builder.Property(attendance => attendance.City)
            .HasColumnName("city")
            .HasMaxLength(120);

        builder.Property(attendance => attendance.Church)
            .HasColumnName("church")
            .HasMaxLength(160);

        builder.Property(attendance => attendance.Pastor)
            .HasColumnName("pastor")
            .HasMaxLength(160);

        builder.Property(attendance => attendance.AttendanceType)
            .HasColumnName("attendance_type")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(attendance => attendance.ReturnNumber)
            .HasColumnName("return_number");

        builder.OwnsOne(attendance => attendance.VitalSigns, vitalSigns =>
        {
            vitalSigns.Property(value => value.SystolicPressure)
                .HasColumnName("systolic_pressure");

            vitalSigns.Property(value => value.DiastolicPressure)
                .HasColumnName("diastolic_pressure");

            vitalSigns.Property(value => value.Temperature)
                .HasColumnName("temperature")
                .HasPrecision(5, 2);

            vitalSigns.Property(value => value.BloodGlucose)
                .HasColumnName("blood_glucose")
                .HasPrecision(7, 2);

            vitalSigns.Property(value => value.OxygenSaturation)
                .HasColumnName("oxygen_saturation");

            vitalSigns.Property(value => value.HeartRate)
                .HasColumnName("heart_rate");
        });

        builder.Property(attendance => attendance.ChiefComplaint)
            .HasColumnName("chief_complaint");

        builder.Property(attendance => attendance.PreviousPathologicalHistory)
            .HasColumnName("previous_pathological_history");

        builder.Property(attendance => attendance.CurrentDiseaseHistory)
            .HasColumnName("current_disease_history");

        builder.Property(attendance => attendance.PhysicalExam)
            .HasColumnName("physical_exam");

        builder.Property(attendance => attendance.DiagnosticHypothesis)
            .HasColumnName("diagnostic_hypothesis");

        builder.Property(attendance => attendance.Cid10Code)
            .HasColumnName("cid10_code")
            .HasMaxLength(20);

        builder.Property(attendance => attendance.Cid10Name)
            .HasColumnName("cid10_name");

        builder.Property(attendance => attendance.ResponsibleSignature)
            .HasColumnName("responsible_signature");

        builder.Property(attendance => attendance.TriageResponsibleUserId)
            .HasColumnName("triage_responsible_user_id");

        builder.Property(attendance => attendance.TriageResponsibleName)
            .HasColumnName("triage_responsible_name")
            .HasMaxLength(160);

        builder.Property(attendance => attendance.TriageResponsibleSignature)
            .HasColumnName("triage_responsible_signature");

        builder.Property(attendance => attendance.MedicalResponsibleUserId)
            .HasColumnName("medical_responsible_user_id");

        builder.Property(attendance => attendance.MedicalResponsibleName)
            .HasColumnName("medical_responsible_name")
            .HasMaxLength(160);

        builder.Property(attendance => attendance.MedicalResponsibleSignature)
            .HasColumnName("medical_responsible_signature");

        builder.Property(attendance => attendance.DispensationResponsibleUserId)
            .HasColumnName("dispensation_responsible_user_id");

        builder.Property(attendance => attendance.DispensationResponsibleName)
            .HasColumnName("dispensation_responsible_name")
            .HasMaxLength(160);

        builder.Property(attendance => attendance.DispensationResponsibleSignature)
            .HasColumnName("dispensation_responsible_signature");

        builder.Property(attendance => attendance.HasBackSide)
            .HasColumnName("has_back_side")
            .IsRequired();

        builder.Property(attendance => attendance.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(attendance => attendance.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(attendance => attendance.AppointmentId)
            .IsUnique();

        builder.HasIndex(attendance => attendance.PatientId);

        builder.Navigation(attendance => attendance.Prescriptions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(attendance => attendance.NursingChecks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(attendance => attendance.Dispensations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(attendance => attendance.Cid10Codes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(attendance => attendance.Prescriptions)
            .WithOne()
            .HasForeignKey(item => item.MedicalAttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(attendance => attendance.NursingChecks)
            .WithOne()
            .HasForeignKey(item => item.MedicalAttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(attendance => attendance.Dispensations)
            .WithOne()
            .HasForeignKey(item => item.MedicalAttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(attendance => attendance.Cid10Codes)
            .WithOne()
            .HasForeignKey(item => item.MedicalAttendanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
