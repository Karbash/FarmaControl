using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Audit;

public sealed class AuditLog : Entity
{
    private AuditLog()
    {
    }

    private AuditLog(
        long? userId,
        string? userName,
        string action,
        string entity,
        long? entityId,
        string description)
    {
        UserId = userId;
        UserName = Normalize(userName) ?? "sistema";
        Action = NormalizeRequired(action, nameof(action));
        Entity = NormalizeRequired(entity, nameof(entity));
        EntityId = entityId;
        Description = NormalizeRequired(description, nameof(description));
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long? UserId { get; private set; }

    public string UserName { get; private set; } = "sistema";

    public string Action { get; private set; } = string.Empty;

    public string Entity { get; private set; } = string.Empty;

    public long? EntityId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditLog Create(
        long? userId,
        string? userName,
        string action,
        string entity,
        long? entityId,
        string description)
    {
        return new AuditLog(userId, userName, action, entity, entityId, description);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Campo obrigatorio.", paramName);
        }

        return value.Trim();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
