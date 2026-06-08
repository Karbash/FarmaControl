using FarmaControl.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmaControl.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id");

        builder.Property(user => user.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(user => user.SignaturePasswordHash)
            .HasColumnName("signature_password_hash");

        builder.Property(user => user.SignaturePasswordResetRequired)
            .HasColumnName("signature_password_reset_required")
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasConversion(
                role => role.Value,
                value => UserRole.From(value))
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(user => user.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(user => user.AccessRevokedAt)
            .HasColumnName("access_revoked_at");

        builder.Property(user => user.AccessRevokedByUserId)
            .HasColumnName("access_revoked_by_user_id");

        builder.Property(user => user.AccessRevocationReason)
            .HasColumnName("access_revocation_reason");

        builder.Property(user => user.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(user => user.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasFilter("is_deleted = 0");

        builder.HasIndex(user => user.IsDeleted);

        builder.Navigation(user => user.ModuleAccesses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(user => user.ModuleAccesses)
            .WithOne()
            .HasForeignKey(access => access.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
