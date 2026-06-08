using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Models;
using FarmaControl.Application.Audit.UseCases;
using FarmaControl.Contracts.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "admin,gerente")]
public sealed class AuditController(
    IUseCase<AuditLogFilterModel, Result<IReadOnlyList<AuditLogResponse>>> listUseCase,
    IUseCase<GenerateAuditReportPdfRequest, Result<AuditReportPdfResponse>> pdfUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> List(
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] string? user,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<AuditLogResponse>> result = await listUseCase.ExecuteAsync(
            new AuditLogFilterModel(action, entity, user, startDate, endDate),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf(
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] string? user,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilterModel(action, entity, user, startDate, endDate);
        string generatedBy = User.FindFirstValue(ClaimTypes.Name) ?? "usuario autenticado";

        Result<AuditReportPdfResponse> result = await pdfUseCase.ExecuteAsync(
            new GenerateAuditReportPdfRequest(filter, generatedBy),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            AppError error = result.Error ?? AppError.Validation("Erro desconhecido.");

            return error.Code switch
            {
                "not_found" => NotFound(new { error = error.Message }),
                "forbidden" => Forbid(),
                "conflict" => Conflict(new { error = error.Message }),
                _ => BadRequest(new { error = error.Message })
            };
        }

        return File(result.Value.Content, "application/pdf", result.Value.FileName);
    }
}
