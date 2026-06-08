using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(patient => patient.Id);

        builder.Property(patient => patient.Id).HasColumnName("id");
        builder.Property(patient => patient.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(patient => patient.Cpf).HasColumnName("cpf").HasMaxLength(40);
        builder.Property(patient => patient.BirthDate).HasColumnName("birth_date");
        builder.Property(patient => patient.Sex).HasColumnName("sex").HasMaxLength(40);
        builder.Property(patient => patient.Phone).HasColumnName("phone").HasMaxLength(80);
        builder.Property(patient => patient.Address).HasColumnName("address");
        builder.Property(patient => patient.Notes).HasColumnName("notes");
        builder.Property(patient => patient.Comorbidities).HasColumnName("comorbidities");
        builder.Property(patient => patient.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(patient => patient.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(patient => patient.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(patient => patient.Name);
        builder.HasIndex(patient => patient.Cpf);
        builder.HasIndex(patient => patient.IsActive);
    }
}
