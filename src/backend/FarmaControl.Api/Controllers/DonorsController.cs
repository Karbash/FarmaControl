using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Contracts.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/donors")]
[Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
public sealed class DonorsController(
    IUseCase<NoRequest, IReadOnlyList<DonorResponse>> listUseCase,
    IUseCase<CreateDonorModel, Result<DonorResponse>> createUseCase,
    IUseCase<UpdateDonorCommand, Result<DonorResponse>> updateUseCase,
    IUseCase<DeleteDonorCommand, Result<bool>> deleteUseCase)
    : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DonorResponse>>> List(CancellationToken cancellationToken)
    {
        return Ok(await listUseCase.ExecuteAsync(NoRequest.Instance, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "admin,gerente,movimentacao,entrada,farmaceutico")]
    public async Task<ActionResult<DonorResponse>> Create(
        CreateDonorRequest request,
        CancellationToken cancellationToken)
    {
        Result<DonorResponse> result = await createUseCase.ExecuteAsync(
            CreateDonorModel.FromRequest(request),
            cancellationToken);

        return result.IsSuccess && result.Value is not null
            ? Created(string.Empty, result.Value)
            : ToActionResult(result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult<DonorResponse>> Update(
        long id,
        UpdateDonorRequest request,
        CancellationToken cancellationToken)
    {
        Result<DonorResponse> result = await updateUseCase.ExecuteAsync(
            new UpdateDonorCommand(id, UpdateDonorModel.FromRequest(request)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,gerente")]
    public async Task<ActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        Result<bool> result = await deleteUseCase.ExecuteAsync(new DeleteDonorCommand(id), cancellationToken);
        return ToEmptyActionResult(result);
    }
}
