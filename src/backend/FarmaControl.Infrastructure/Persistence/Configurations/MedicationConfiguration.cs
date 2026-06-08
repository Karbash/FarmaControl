using FarmaControl.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class MedicationConfiguration : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.ToTable("medications");

        builder.HasKey(medication => medication.Id);

        builder.Property(medication => medication.Id).HasColumnName("id");
        builder.Property(medication => medication.GenericName).HasColumnName("generic_name").HasMaxLength(200);
        builder.Property(medication => medication.CommercialName).HasColumnName("commercial_name").HasMaxLength(200);
        builder.Property(medication => medication.TherapeuticClass).HasColumnName("therapeutic_class").HasMaxLength(160);
        builder.Property(medication => medication.PharmaceuticalForm).HasColumnName("pharmaceutical_form").HasMaxLength(120);
        builder.Property(medication => medication.Dosage).HasColumnName("dosage").HasMaxLength(120);
        builder.Property(medication => medication.EntryDate).HasColumnName("entry_date");
        builder.Property(medication => medication.Origin).HasColumnName("origin").HasMaxLength(160);
        builder.Property(medication => medication.OriginId).HasColumnName("origin_id");
        builder.Property(medication => medication.Responsible).HasColumnName("responsible").HasMaxLength(160);
        builder.Property(medication => medication.Manufacturer).HasColumnName("manufacturer").HasMaxLength(160);
        builder.Property(medication => medication.ManufacturerId).HasColumnName("manufacturer_id");
        builder.Property(medication => medication.Batch).HasColumnName("batch").HasMaxLength(120);
        builder.Property(medication => medication.ExpirationDate).HasColumnName("expiration_date");
        builder.Property(medication => medication.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(medication => medication.Unit).HasColumnName("unit").HasMaxLength(80);
        builder.Property(medication => medication.Location).HasColumnName("location").HasMaxLength(160);
        builder.Property(medication => medication.LocationId).HasColumnName("location_id");
        builder.Property(medication => medication.MinimumQuantity).HasColumnName("minimum_quantity").IsRequired();
        builder.Property(medication => medication.IsControlled).HasColumnName("is_controlled").IsRequired();
        builder.Property(medication => medication.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(medication => medication.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(medication => medication.GenericName);
        builder.HasIndex(medication => medication.CommercialName);
        builder.HasIndex(medication => medication.Batch);
        builder.HasIndex(medication => medication.ExpirationDate);
        builder.HasIndex(medication => medication.LocationId);
        builder.HasIndex(medication => medication.OriginId);
        builder.HasIndex(medication => medication.ManufacturerId);
    }
}
