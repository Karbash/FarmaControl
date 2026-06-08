using FarmaControl.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class DonorConfiguration : IEntityTypeConfiguration<Donor>
{
    public void Configure(EntityTypeBuilder<Donor> builder)
    {
        builder.ToTable("donors");
        builder.HasKey(donor => donor.Id);
        builder.Property(donor => donor.Id).HasColumnName("id");
        builder.Property(donor => donor.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(donor => donor.Phone).HasColumnName("phone").HasMaxLength(80);
        builder.Property(donor => donor.Notes).HasColumnName("notes");
        builder.Property(donor => donor.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(donor => donor.Name);
    }
}

public sealed class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("manufacturers");
        builder.HasKey(manufacturer => manufacturer.Id);
        builder.Property(manufacturer => manufacturer.Id).HasColumnName("id");
        builder.Property(manufacturer => manufacturer.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(manufacturer => manufacturer.Cnpj).HasColumnName("cnpj").HasMaxLength(40);
        builder.Property(manufacturer => manufacturer.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(manufacturer => manufacturer.Name);
    }
}

public sealed class StockLocationConfiguration : IEntityTypeConfiguration<StockLocation>
{
    public void Configure(EntityTypeBuilder<StockLocation> builder)
    {
        builder.ToTable("stock_locations");
        builder.HasKey(location => location.Id);
        builder.Property(location => location.Id).HasColumnName("id");
        builder.Property(location => location.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(location => location.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(location => location.Name).IsUnique();
    }
}
