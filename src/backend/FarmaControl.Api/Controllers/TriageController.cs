using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Triage.Models;
using FarmaControl.Application.Care.Triage.UseCases;
using FarmaControl.Contracts.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/triage")]
[Authorize(Roles = "admin,gerente,medico,enfermeira,farmaceutico")]
public sealed class TriageController(
    IUseCase<ListTriageRecordsRequest, Result<IReadOnlyList<TriageRecordResponse>>> listUseCase,
    IUseCase<TriageRecordInputModel, Result<TriageRecordResponse>> createUseCase,
    IUseCase<UpdateTriageRecordCommand, Result<TriageRecordResponse>> updateUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TriageRecordResponse>>> List(
        [FromQuery] long appointmentId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<TriageRecordResponse>> result = await listUseCase.ExecuteAsync(
            new ListTriageRecordsRequest(appointmentId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<TriageRecordResponse>> Create(
        CreateTriageRecordRequest request,
        CancellationToken cancellationToken)
    {
        Result<TriageRecordResponse> result = await createUseCase.ExecuteAsync(
            TriageRecordInputModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Created(string.Empty, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<TriageRecordResponse>> Update(
        long id,
        UpdateTriageRecordRequest request,
        CancellationToken cancellationToken)
    {
        Result<TriageRecordResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateTriageRecordCommand(id, TriageRecordInputModel.FromRequest(1, request)),
            cancellationToken);

        return ToActionResult(result);
    }
}
