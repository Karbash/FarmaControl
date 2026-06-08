using FarmaControl.Domain.Audit;

namespace FarmaControl.Application.Audit.Abstractions;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> ListAsync(
        string? action,
        string? entity,
        string? user,
        DateOnly? startDate,
        DateOnly? endDate,
        int limit,
        CancellationToken cancellationToken);

    Task AddAsync(AuditLog log, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
