using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Domain.Audit;
using Microsoft.Extensions.Logging;

namespace FarmaControl.Infrastructure.Audit;

public sealed class EfAuditLogger(
    IAuditLogRepository auditLogs,
    ILogger<EfAuditLogger> logger) : IAuditLogger
{
    public async Task LogAsync(
        long? userId,
        string? userName,
        string action,
        string entity,
        long? entityId,
        string description,
        CancellationToken cancellationToken)
    {
        try
        {
            AuditLog log = AuditLog.Create(userId, userName, action, entity, entityId, description);
            await auditLogs.AddAsync(log, cancellationToken);
            await auditLogs.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Audit log failed for {Action} {Entity} {EntityId}", action, entity, entityId);
        }
    }
}
