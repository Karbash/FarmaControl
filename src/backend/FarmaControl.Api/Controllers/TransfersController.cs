using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize(Roles = "admin,gerente,movimentacao,saida,farmaceutico")]
public sealed class TransfersController(
    IUseCase<TransferMedicationModel, Result<TransferMedicationResponse>> transferUseCase)
    : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TransferMedicationResponse>> Transfer(
        TransferMedicationRequest request,
        CancellationToken cancellationToken)
    {
        Result<TransferMedicationResponse> result = await transferUseCase.ExecuteAsync(
            TransferMedicationModel.FromRequest(request),
            cancellationToken);

        return ToActionResult(result);
    }
}
