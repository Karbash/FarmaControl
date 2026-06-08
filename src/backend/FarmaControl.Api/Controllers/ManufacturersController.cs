using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/manufacturers")]
[Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
public sealed class ManufacturersController(
    IUseCase<NoRequest, IReadOnlyList<ManufacturerResponse>> listUseCase,
    IUseCase<CreateManufacturerModel, Result<ManufacturerResponse>> createUseCase,
    IUseCase<UpdateManufacturerCommand, Result<ManufacturerResponse>> updateUseCase,
    IUseCase<DeleteManufacturerCommand, Result<bool>> deleteUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManufacturerResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await listUseCase.ExecuteAsync(NoRequest.Instance, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
    public async Task<ActionResult<ManufacturerResponse>> Create(
        CreateManufacturerRequest request,
        CancellationToken cancellationToken)
    {
        Result<ManufacturerResponse> result = await createUseCase.ExecuteAsync(
            CreateManufacturerModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Created(string.Empty, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult<ManufacturerResponse>> Update(
        long id,
        UpdateManufacturerRequest request,
        CancellationToken cancellationToken)
    {
        Result<ManufacturerResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateManufacturerCommand(id, UpdateManufacturerModel.FromRequest(request)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUseCase.ExecuteAsync(
            new DeleteManufacturerCommand(id),
            cancellationToken);

        return ToEmptyActionResult(result);
    }
}
