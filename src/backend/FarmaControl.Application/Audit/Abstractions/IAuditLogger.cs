namespace FarmaControl.Application.Audit.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        long? userId,
        string? userName,
        string action,
        string entity,
        long? entityId,
        string description,
        CancellationToken cancellationToken);
}
