using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Audit;
using FarmaControl.Domain.Audit;

namespace FarmaControl.Application.Audit.Models;

public sealed record AuditLogFilterModel(
    string? Action,
    string? Entity,
    string? User,
    DateOnly? StartDate,
    DateOnly? EndDate) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
        {
            return [AppError.Validation("Data inicial nao pode ser maior que data final.")];
        }

        return [];
    }
}

public static class AuditLogModel
{
    public static AuditLogResponse FromDomain(AuditLog log)
    {
        return new AuditLogResponse(
            log.Id,
            log.UserId,
            log.UserName,
            log.Action,
            log.Entity,
            log.EntityId,
            log.Description,
            log.CreatedAt);
    }
}
