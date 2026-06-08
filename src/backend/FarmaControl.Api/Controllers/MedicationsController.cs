using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/medications")]
[Authorize(Roles = "admin,gerente,medico,enfermeira,farmaceutico,movimentacao,entrada,saida,visualizacao")]
public sealed class MedicationsController(
    IUseCase<NoRequest, IReadOnlyList<MedicationResponse>> listUseCase,
    IUseCase<GetMedicationRequest, Result<MedicationResponse>> getUseCase,
    IUseCase<CreateMedicationCommand, Result<MedicationResponse>> createUseCase,
    IUseCase<UpdateMedicationCommand, Result<MedicationResponse>> updateUseCase,
    IUseCase<DeleteMedicationCommand, Result<bool>> deleteUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MedicationResponse>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<MedicationResponse> result =
            await listUseCase.ExecuteAsync(NoRequest.Instance, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MedicationResponse>> Get(long id, CancellationToken cancellationToken)
    {
        Result<MedicationResponse> result = await getUseCase.ExecuteAsync(
            new GetMedicationRequest(id),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
    public async Task<ActionResult<MedicationResponse>> Create(
        CreateMedicationRequest request,
        CancellationToken cancellationToken)
    {
        Result<MedicationResponse> result = await createUseCase.ExecuteAsync(
            new CreateMedicationCommand(
                CurrentUserId(),
                MedicationInputModel.FromRequest(request),
                request.SignaturePassword),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
    public async Task<ActionResult<MedicationResponse>> Update(
        long id,
        UpdateMedicationRequest request,
        CancellationToken cancellationToken)
    {
        Result<MedicationResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateMedicationCommand(id, MedicationInputModel.FromRequest(request)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUseCase.ExecuteAsync(
            new DeleteMedicationCommand(id),
            cancellationToken);

        return ToEmptyActionResult(result);
    }
}
