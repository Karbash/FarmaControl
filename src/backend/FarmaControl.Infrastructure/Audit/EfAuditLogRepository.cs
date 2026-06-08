using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Domain.Audit;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Audit;

public sealed class EfAuditLogRepository(FarmaControlDbContext dbContext) : IAuditLogRepository
{
    public async Task<IReadOnlyList<AuditLog>> ListAsync(
        string? action,
        string? entity,
        string? user,
        DateOnly? startDate,
        DateOnly? endDate,
        int limit,
        CancellationToken cancellationToken)
    {
        IQueryable<AuditLog> query = dbContext.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(log => log.Action == action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entity))
        {
            query = query.Where(log => log.Entity == entity.Trim());
        }

        if (!string.IsNullOrWhiteSpace(user))
        {
            string term = user.Trim();
            query = query.Where(log => log.UserName.Contains(term));
        }

        if (startDate.HasValue)
        {
            var start = new DateTimeOffset(startDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(log => log.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = new DateTimeOffset(
                endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(log => log.CreatedAt < endExclusive);
        }

        int boundedLimit = Math.Clamp(limit, 1, 500);

        return await query
            .OrderByDescending(log => log.Id)
            .Take(boundedLimit)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken)
    {
        await dbContext.AuditLogs.AddAsync(log, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
