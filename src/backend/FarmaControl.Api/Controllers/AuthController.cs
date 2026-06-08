using FarmaControl.Api.Auth;
using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Application.Users.UseCases;
using FarmaControl.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IUseCase<LoginModel, Result<AuthenticatedUserResponse>> loginUseCase,
    IUseCase<ChangePasswordModel, Result<AuthenticatedUserResponse>> changePasswordUseCase,
    IUseCase<ChangeSignaturePasswordModel, Result<AuthenticatedUserResponse>> changeSignaturePasswordUseCase,
    IUseCase<GetCurrentUserRequest, Result<AuthenticatedUserResponse>> currentUserUseCase,
    JwtTokenService jwtTokenService)
    : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedUserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthenticatedUserResponse> result = await loginUseCase.ExecuteAsync(
            LoginModel.FromRequest(request),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return Unauthorized(new { error = result.Error?.Message ?? "Login invalido." });
        }

        JwtTokenResult token = jwtTokenService.CreateToken(result.Value);

        return Ok(result.Value with
        {
            AccessToken = token.AccessToken,
            AccessTokenExpiresAt = token.ExpiresAt
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthenticatedUserResponse>> Me(CancellationToken cancellationToken)
    {
        Result<AuthenticatedUserResponse> result = await currentUserUseCase.ExecuteAsync(
            new GetCurrentUserRequest(CurrentUserId()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("password")]
    [Authorize]
    public async Task<ActionResult<AuthenticatedUserResponse>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthenticatedUserResponse> result = await changePasswordUseCase.ExecuteAsync(
            ChangePasswordModel.FromRequest(CurrentUserId(), request),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("signature-password")]
    [Authorize]
    public async Task<ActionResult<AuthenticatedUserResponse>> ChangeSignaturePassword(
        ChangeSignaturePasswordRequest request,
        CancellationToken cancellationToken)
    {
        Result<AuthenticatedUserResponse> result = await changeSignaturePasswordUseCase.ExecuteAsync(
            ChangeSignaturePasswordModel.FromRequest(CurrentUserId(), request),
            cancellationToken);

        return ToActionResult(result);
    }
}
