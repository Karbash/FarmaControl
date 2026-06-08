using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Health;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(
    IDatabaseHealthCheck databaseHealthCheck,
    IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        bool database = await databaseHealthCheck.CanConnectAsync(cancellationToken);
        var status = database ? "ok" : "degraded";

        return Ok(new HealthResponse(
            status,
            environment.EnvironmentName,
            database,
            DateTimeOffset.UtcNow));
    }
}
