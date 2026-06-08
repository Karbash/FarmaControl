using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/stock-movements")]
[Authorize(Roles = "admin,gerente,movimentacao,entrada,saida,farmaceutico")]
public sealed class StockMovementsController(
    IUseCase<NoRequest, IReadOnlyList<StockMovementResponse>> listUseCase,
    IUseCase<StockMovementInputModel, Result<StockMovementResponse>> createUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockMovementResponse>>> List(
        CancellationToken cancellationToken)
    {
        return Ok(await listUseCase.ExecuteAsync(NoRequest.Instance, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<StockMovementResponse>> Create(
        CreateStockMovementRequest request,
        CancellationToken cancellationToken)
    {
        if (!CanCreateMovement(request.Type))
        {
            return Forbid();
        }

        Result<StockMovementResponse> result = await createUseCase.ExecuteAsync(
            StockMovementInputModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Created(string.Empty, result.Value)
            : ToActionResult(result);
    }

    private bool CanCreateMovement(string type)
    {
        if (string.Equals(type?.Trim(), "entrada", StringComparison.OrdinalIgnoreCase))
        {
            return HasAnyRole("admin", "gerente", "movimentacao", "entrada", "farmaceutico");
        }

        return HasAnyRole("admin", "gerente", "movimentacao", "saida", "farmaceutico");
    }

    private bool HasAnyRole(params string[] roles)
    {
        return roles.Any(User.IsInRole);
    }
}
