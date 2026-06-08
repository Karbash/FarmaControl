using FarmaControl.Domain.Care;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class Cid10CodeConfiguration : IEntityTypeConfiguration<Cid10Code>
{
    public void Configure(EntityTypeBuilder<Cid10Code> builder)
    {
        builder.ToTable("cid10_codes");

        builder.HasKey(code => code.Id);

        builder.Property(code => code.Id).HasColumnName("id");
        builder.Property(code => code.Code)
            .HasColumnName("code")
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(code => code.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(code => code.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(code => code.Code).IsUnique();
        builder.HasIndex(code => code.Name);
        builder.HasIndex(code => new { code.Code, code.Name });
    }
}
