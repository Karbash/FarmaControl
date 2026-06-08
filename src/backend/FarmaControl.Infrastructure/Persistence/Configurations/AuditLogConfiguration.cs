using FarmaControl.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id).HasColumnName("id");
        builder.Property(log => log.UserId).HasColumnName("user_id");
        builder.Property(log => log.UserName).HasColumnName("user_name").HasMaxLength(200).IsRequired();
        builder.Property(log => log.Action).HasColumnName("action").HasMaxLength(80).IsRequired();
        builder.Property(log => log.Entity).HasColumnName("entity").HasMaxLength(120).IsRequired();
        builder.Property(log => log.EntityId).HasColumnName("entity_id");
        builder.Property(log => log.Description).HasColumnName("description").IsRequired();
        builder.Property(log => log.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(log => log.CreatedAt);
        builder.HasIndex(log => log.Action);
        builder.HasIndex(log => log.Entity);
        builder.HasIndex(log => log.UserName);
    }
}
