using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Audit.Models;
using FarmaControl.Contracts.Audit;

namespace FarmaControl.Application.Audit.UseCases;

public sealed class ListAuditLogsUseCase(IAuditLogRepository auditLogs)
    : IUseCase<AuditLogFilterModel, Result<IReadOnlyList<AuditLogResponse>>>
{
    private const int Limit = 500;

    public async Task<Result<IReadOnlyList<AuditLogResponse>>> ExecuteAsync(
        AuditLogFilterModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<IReadOnlyList<AuditLogResponse>>.Failure(errors.FirstOrDefaultError());
        }

        var result = await auditLogs.ListAsync(
            request.Action,
            request.Entity,
            request.User,
            request.StartDate,
            request.EndDate,
            Limit,
            cancellationToken);

        return Result<IReadOnlyList<AuditLogResponse>>.Success(
            result.Select(AuditLogModel.FromDomain).ToArray());
    }
}
