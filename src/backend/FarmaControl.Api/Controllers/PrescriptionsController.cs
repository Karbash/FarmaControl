using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Prescriptions.Models;
using FarmaControl.Application.Care.Prescriptions.UseCases;
using FarmaControl.Contracts.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/prescriptions")]
public sealed class PrescriptionsController(
    IUseCase<ListPrescriptionsRequest, IReadOnlyList<PrescriptionResponse>> listUseCase,
    IUseCase<PrescriptionInputModel, Result<PrescriptionResponse>> createUseCase,
    IUseCase<DeletePrescriptionCommand, Result<bool>> deleteUseCase,
    IUseCase<DispensePrescriptionModel, Result<DispensePrescriptionResponse>> dispenseUseCase)
    : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = "admin,gerente,medico,enfermeira,farmaceutico,movimentacao,saida")]
    public async Task<ActionResult<IReadOnlyList<PrescriptionResponse>>> List(
        [FromQuery] long? medicalRecordId,
        [FromQuery] long? patientId,
        [FromQuery] bool? isDispensed,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PrescriptionResponse> result = await listUseCase.ExecuteAsync(
            new ListPrescriptionsRequest(medicalRecordId, patientId, isDispensed),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin,gerente,medico,enfermeira,farmaceutico")]
    public async Task<ActionResult<PrescriptionResponse>> Create(
        CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        Result<PrescriptionResponse> result = await createUseCase.ExecuteAsync(
            PrescriptionInputModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Created(string.Empty, result.Value)
            : ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,gerente,medico,enfermeira,farmaceutico")]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUseCase.ExecuteAsync(
            new DeletePrescriptionCommand(id),
            cancellationToken);

        return ToEmptyActionResult(result);
    }

    [HttpPost("{id:long}/dispense")]
    [Authorize(Roles = "admin,gerente,movimentacao,saida,farmaceutico")]
    public async Task<ActionResult<DispensePrescriptionResponse>> Dispense(
        long id,
        DispensePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        Result<DispensePrescriptionResponse> result = await dispenseUseCase.ExecuteAsync(
            DispensePrescriptionModel.FromRequest(id, request),
            cancellationToken);

        return ToActionResult(result);
    }
}
