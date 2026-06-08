using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Models;
using FarmaControl.Application.Care.MedicalRecords.UseCases;
using FarmaControl.Contracts.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/medical-records")]
[Authorize(Roles = "admin,gerente,medico,enfermeira,farmaceutico")]
public sealed class MedicalRecordsController(
    IUseCase<ListMedicalRecordsRequest, IReadOnlyList<MedicalRecordResponse>> listUseCase,
    IUseCase<GetMedicalRecordRequest, Result<MedicalRecordResponse>> getUseCase,
    IUseCase<MedicalRecordInputModel, Result<MedicalRecordResponse>> createUseCase,
    IUseCase<UpdateMedicalRecordCommand, Result<MedicalRecordResponse>> updateUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MedicalRecordResponse>>> List(
        [FromQuery] long? appointmentId,
        [FromQuery] long? patientId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MedicalRecordResponse> result = await listUseCase.ExecuteAsync(
            new ListMedicalRecordsRequest(appointmentId, patientId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MedicalRecordResponse>> Get(long id, CancellationToken cancellationToken)
    {
        Result<MedicalRecordResponse> result = await getUseCase.ExecuteAsync(
            new GetMedicalRecordRequest(id),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<MedicalRecordResponse>> Create(
        CreateMedicalRecordRequest request,
        CancellationToken cancellationToken)
    {
        Result<MedicalRecordResponse> result = await createUseCase.ExecuteAsync(
            MedicalRecordInputModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<MedicalRecordResponse>> Update(
        long id,
        UpdateMedicalRecordRequest request,
        CancellationToken cancellationToken)
    {
        Result<MedicalRecordResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateMedicalRecordCommand(id, request),
            cancellationToken);

        return ToActionResult(result);
    }
}
