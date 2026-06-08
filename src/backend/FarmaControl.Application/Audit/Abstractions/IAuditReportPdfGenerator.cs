using FarmaControl.Application.Audit.Models;
using FarmaControl.Domain.Audit;

namespace FarmaControl.Application.Audit.Abstractions;

public interface IAuditReportPdfGenerator
{
    Task<byte[]> GenerateAsync(
        IReadOnlyList<AuditLog> logs,
        AuditLogFilterModel filter,
        string generatedBy,
        CancellationToken cancellationToken);
}
