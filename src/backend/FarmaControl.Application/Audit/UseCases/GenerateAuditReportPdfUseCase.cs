using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Audit.Models;
using FarmaControl.Domain.Audit;

namespace FarmaControl.Application.Audit.UseCases;

public sealed record GenerateAuditReportPdfRequest(
    AuditLogFilterModel Filter,
    string GeneratedBy);

public sealed record AuditReportPdfResponse(
    byte[] Content,
    string FileName);

public sealed class GenerateAuditReportPdfUseCase(
    IAuditLogRepository auditLogs,
    IAuditReportPdfGenerator pdfGenerator)
    : IUseCase<GenerateAuditReportPdfRequest, Result<AuditReportPdfResponse>>
{
    private const int Limit = 1000;

    public async Task<Result<AuditReportPdfResponse>> ExecuteAsync(
        GenerateAuditReportPdfRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Filter.Validate();
        if (errors.HasErrors())
        {
            return Result<AuditReportPdfResponse>.Failure(errors.FirstOrDefaultError());
        }

        if (!request.Filter.StartDate.HasValue || !request.Filter.EndDate.HasValue)
        {
            return Result<AuditReportPdfResponse>.Failure(
                AppError.Validation("Informe data inicial e data final para gerar o PDF de auditoria."));
        }

        IReadOnlyList<AuditLog> result = await auditLogs.ListAsync(
            request.Filter.Action,
            request.Filter.Entity,
            request.Filter.User,
            request.Filter.StartDate,
            request.Filter.EndDate,
            Limit,
            cancellationToken);

        byte[] content = await pdfGenerator.GenerateAsync(
            result,
            request.Filter,
            request.GeneratedBy,
            cancellationToken);

        if (content.Length == 0)
        {
            return Result<AuditReportPdfResponse>.Failure(
                AppError.Validation("PDF nao foi gerado."));
        }

        string start = request.Filter.StartDate.Value.ToString("yyyyMMdd");
        string end = request.Filter.EndDate.Value.ToString("yyyyMMdd");
        string fileName = $"relatorio-auditoria-{start}-{end}.pdf";

        return Result<AuditReportPdfResponse>.Success(new AuditReportPdfResponse(content, fileName));
    }
}
