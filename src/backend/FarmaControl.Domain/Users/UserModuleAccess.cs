using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Users;

public sealed class UserModuleAccess : Entity
{
    private UserModuleAccess()
    {
    }

    private UserModuleAccess(string module, long grantedByUserId)
    {
        Module = NormalizeModule(module);
        GrantedByUserId = grantedByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long UserId { get; private set; }

    public string Module { get; private set; } = string.Empty;

    public bool IsRevoked { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public long? RevokedByUserId { get; private set; }

    public string? RevocationReason { get; private set; }

    public long GrantedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static UserModuleAccess Grant(string module, long grantedByUserId)
    {
        if (grantedByUserId <= 0)
        {
            throw new ArgumentException("Usuario que concedeu acesso e obrigatorio.", nameof(grantedByUserId));
        }

        return new UserModuleAccess(module, grantedByUserId);
    }

    public void Revoke(long revokedByUserId, string? reason)
    {
        if (revokedByUserId <= 0)
        {
            throw new ArgumentException("Usuario que revogou acesso e obrigatorio.", nameof(revokedByUserId));
        }

        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByUserId = revokedByUserId;
        RevocationReason = reason;
    }

    public void Restore()
    {
        IsRevoked = false;
        RevokedAt = null;
        RevokedByUserId = null;
        RevocationReason = null;
    }

    private static string NormalizeModule(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            throw new ArgumentException("Modulo e obrigatorio.", nameof(module));
        }

        return module.Trim().ToLowerInvariant();
    }
}
