using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/stock-locations")]
[Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
public sealed class StockLocationsController(
    IUseCase<NoRequest, IReadOnlyList<StockLocationResponse>> listUseCase,
    IUseCase<CreateStockLocationModel, Result<StockLocationResponse>> createUseCase,
    IUseCase<UpdateStockLocationCommand, Result<StockLocationResponse>> updateUseCase,
    IUseCase<DeleteStockLocationCommand, Result<bool>> deleteUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockLocationResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await listUseCase.ExecuteAsync(NoRequest.Instance, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
    public async Task<ActionResult<StockLocationResponse>> Create(
        CreateStockLocationRequest request,
        CancellationToken cancellationToken)
    {
        Result<StockLocationResponse> result = await createUseCase.ExecuteAsync(
            CreateStockLocationModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Created(string.Empty, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult<StockLocationResponse>> Update(
        long id,
        UpdateStockLocationRequest request,
        CancellationToken cancellationToken)
    {
        Result<StockLocationResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateStockLocationCommand(id, UpdateStockLocationModel.FromRequest(request)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUseCase.ExecuteAsync(
            new DeleteStockLocationCommand(id),
            cancellationToken);

        return ToEmptyActionResult(result);
    }
}
