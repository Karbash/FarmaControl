using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.Cid10;
using FarmaControl.Contracts.Care;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/cid10")]
[Authorize]
public sealed class Cid10Controller(
    IUseCase<SearchCid10Request, IReadOnlyList<Cid10Response>> searchUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Cid10Response>>> Search(
        [FromQuery(Name = "q")] string? query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Cid10Response> result = await searchUseCase.ExecuteAsync(
            new SearchCid10Request(query),
            cancellationToken);

        return Ok(result);
    }
}
