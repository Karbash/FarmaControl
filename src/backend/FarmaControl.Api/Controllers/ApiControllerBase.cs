using System.Security.Claims;
using FarmaControl.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected long CurrentUserId()
    {
        string? value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out long id) ? id : 0;
    }

    protected ActionResult<T> ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(result.Value);
        }

        AppError error = result.Error ?? AppError.Validation("Erro desconhecido.");

        return error.Code switch
        {
            "not_found" => NotFound(new { error = error.Message }),
            "forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { error = error.Message }),
            "conflict" => Conflict(new { error = error.Message }),
            _ => BadRequest(new { error = error.Message })
        };
    }

    protected ActionResult ToEmptyActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { ok = true });
        }

        AppError error = result.Error ?? AppError.Validation("Erro desconhecido.");

        return error.Code switch
        {
            "not_found" => NotFound(new { error = error.Message }),
            "forbidden" => StatusCode(StatusCodes.Status403Forbidden, new { error = error.Message }),
            "conflict" => Conflict(new { error = error.Message }),
            _ => BadRequest(new { error = error.Message })
        };
    }
}
