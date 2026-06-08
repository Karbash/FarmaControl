using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Users;

public sealed class User : Entity
{
    private readonly List<UserModuleAccess> moduleAccesses = [];

    private User()
    {
    }

    private User(string name, string email, string passwordHash, UserRole role)
    {
        Name = NormalizeName(name);
        Email = NormalizeEmail(email);
        PasswordHash = RequirePasswordHash(passwordHash);
        Role = role;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string? SignaturePasswordHash { get; private set; }

    public bool SignaturePasswordResetRequired { get; private set; } = true;

    public UserRole Role { get; private set; } = UserRole.Visualizacao;

    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? AccessRevokedAt { get; private set; }

    public long? AccessRevokedByUserId { get; private set; }

    public string? AccessRevocationReason { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public long? DeletedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<UserModuleAccess> ModuleAccesses => moduleAccesses.AsReadOnly();

    public bool CanAuthenticate =>
        IsActive &&
        !IsDeleted &&
        AccessRevokedAt is null;

    public bool CanSign =>
        CanAuthenticate &&
        !SignaturePasswordResetRequired &&
        !string.IsNullOrWhiteSpace(SignaturePasswordHash);

    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        return new User(name, email, passwordHash, role);
    }

    public void UpdateProfile(string name, string email, UserRole role)
    {
        EnsureNotDeleted();

        Name = NormalizeName(name);
        Email = NormalizeEmail(email);
        Role = role;
        Touch();
    }

    public void ChangePasswordHash(string passwordHash)
    {
        EnsureNotDeleted();

        PasswordHash = RequirePasswordHash(passwordHash);
        Touch();
    }

    public void ChangeSignaturePasswordHash(string signaturePasswordHash)
    {
        EnsureNotDeleted();

        SignaturePasswordHash = RequirePasswordHash(signaturePasswordHash);
        SignaturePasswordResetRequired = false;
        Touch();
    }

    public void ResetSignaturePassword()
    {
        EnsureNotDeleted();

        SignaturePasswordHash = null;
        SignaturePasswordResetRequired = true;
        Touch();
    }

    public void Deactivate()
    {
        EnsureNotDeleted();

        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        EnsureNotDeleted();

        IsActive = true;
        Touch();
    }

    public void RevokeAccess(long revokedByUserId, string? reason)
    {
        EnsureNotDeleted();

        if (revokedByUserId <= 0)
        {
            throw new ArgumentException("Usuario que revogou acesso e obrigatorio.", nameof(revokedByUserId));
        }

        AccessRevokedAt = DateTimeOffset.UtcNow;
        AccessRevokedByUserId = revokedByUserId;
        AccessRevocationReason = reason;
        Touch();
    }

    public void RestoreAccess()
    {
        EnsureNotDeleted();

        AccessRevokedAt = null;
        AccessRevokedByUserId = null;
        AccessRevocationReason = null;
        Touch();
    }

    public void SoftDelete(long deletedByUserId)
    {
        if (deletedByUserId <= 0)
        {
            throw new ArgumentException("Usuario que deletou e obrigatorio.", nameof(deletedByUserId));
        }

        IsActive = false;
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedByUserId = deletedByUserId;
        AccessRevokedAt ??= DeletedAt;
        AccessRevokedByUserId ??= deletedByUserId;
        AccessRevocationReason ??= "Usuario removido por soft delete.";
        Touch();
    }

    public UserModuleAccess GrantModule(string module, long grantedByUserId)
    {
        EnsureNotDeleted();

        string normalizedModule = NormalizeModule(module);
        UserModuleAccess? current = moduleAccesses
            .FirstOrDefault(access => access.Module == normalizedModule);

        if (current is not null)
        {
            current.Restore();
            Touch();
            return current;
        }

        UserModuleAccess access = UserModuleAccess.Grant(normalizedModule, grantedByUserId);
        moduleAccesses.Add(access);
        Touch();

        return access;
    }

    public void RevokeModule(string module, long revokedByUserId, string? reason)
    {
        EnsureNotDeleted();

        string normalizedModule = NormalizeModule(module);
        UserModuleAccess? current = moduleAccesses
            .FirstOrDefault(access => access.Module == normalizedModule);

        if (current is null)
        {
            return;
        }

        current.Revoke(revokedByUserId, reason);
        Touch();
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Usuario deletado nao pode ser alterado.");
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome e obrigatorio.", nameof(name));
        }

        return name.Trim();
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email e obrigatorio.", nameof(email));
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string RequirePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Hash da senha e obrigatorio.", nameof(passwordHash));
        }

        return passwordHash;
    }

    private static string NormalizeModule(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentException("Modulo e obrigatorio.", nameof(module));
        }

        return module.Trim().ToLowerInvariant();
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
