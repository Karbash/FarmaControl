using FarmaControl.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class UserModuleAccessConfiguration : IEntityTypeConfiguration<UserModuleAccess>
{
    public void Configure(EntityTypeBuilder<UserModuleAccess> builder)
    {
        builder.ToTable("user_module_accesses");

        builder.HasKey(access => access.Id);

        builder.Property(access => access.Id)
            .HasColumnName("id");

        builder.Property(access => access.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(access => access.Module)
            .HasColumnName("module")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(access => access.IsRevoked)
            .HasColumnName("is_revoked")
            .IsRequired();

        builder.Property(access => access.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(access => access.RevokedByUserId)
            .HasColumnName("revoked_by_user_id");

        builder.Property(access => access.RevocationReason)
            .HasColumnName("revocation_reason");

        builder.Property(access => access.GrantedByUserId)
            .HasColumnName("granted_by_user_id")
            .IsRequired();

        builder.Property(access => access.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(access => new { access.UserId, access.Module })
            .IsUnique();
    }
}
