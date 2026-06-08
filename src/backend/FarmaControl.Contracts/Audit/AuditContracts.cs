namespace FarmaControl.Contracts.Audit;

public sealed record AuditLogResponse(
    long Id,
    long? UserId,
    string UserName,
    string Action,
    string Entity,
    long? EntityId,
    string Description,
    DateTimeOffset CreatedAt);
