using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Application.Users.UseCases;
using FarmaControl.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmaControl.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(
    IUseCase<ListUsersRequest, IReadOnlyList<UserResponse>> listUsersUseCase,
    IUseCase<GetUserRequest, Result<UserResponse>> getUserUseCase,
    IUseCase<CreateUserCommand, Result<UserResponse>> createUserUseCase,
    IUseCase<UpdateUserCommand, Result<UserResponse>> updateUserUseCase,
    IUseCase<SoftDeleteUserCommand, Result<UserResponse>> softDeleteUserUseCase,
    IUseCase<RevokeUserAccessModel, Result<UserResponse>> revokeUserAccessUseCase,
    IUseCase<RestoreUserAccessCommand, Result<UserResponse>> restoreUserAccessUseCase,
    IUseCase<ResetSignaturePasswordCommand, Result<UserResponse>> resetSignaturePasswordUseCase,
    IUseCase<GrantUserModuleCommand, Result<UserResponse>> grantUserModuleUseCase,
    IUseCase<RevokeUserModuleModel, Result<UserResponse>> revokeUserModuleUseCase,
    IUseCase<NoRequest, IReadOnlyList<CareTeamUserResponse>> listCareTeamUseCase,
    IUseCase<ListResponsibleUsersRequest, IReadOnlyList<ResponsibleUserResponse>> listResponsibleUsersUseCase)
    : ApiControllerBase
{
    [HttpGet("care-team")]
    [Authorize(Roles = "admin,gerente,medico,enfermeira,farmaceutico")]
    public async Task<ActionResult<IReadOnlyList<CareTeamUserResponse>>> CareTeam(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CareTeamUserResponse> result =
            await listCareTeamUseCase.ExecuteAsync(NoRequest.Instance, cancellationToken);

        return Ok(result);
    }

    [HttpGet("responsibles")]
    [Authorize(Roles = "admin,gerente,atendimento,atendente,medico,enfermeira,farmaceutico,movimentacao,entrada,saida")]
    public async Task<ActionResult<IReadOnlyList<ResponsibleUserResponse>>> Responsibles(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ResponsibleUserResponse> result =
            await listResponsibleUsersUseCase.ExecuteAsync(new ListResponsibleUsersRequest(), cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(
        [FromQuery] bool includeDeleted,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UserResponse> result = await listUsersUseCase.ExecuteAsync(
            new ListUsersRequest(includeDeleted),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> Get(long id, CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await getUserUseCase.ExecuteAsync(
            new GetUserRequest(id),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await createUserUseCase.ExecuteAsync(
            new CreateUserCommand(CurrentUserId(), CreateUserModel.FromRequest(request)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> Update(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await updateUserUseCase.ExecuteAsync(
            new UpdateUserCommand(id, CurrentUserId(), UpdateUserModel.FromRequest(request)),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> SoftDelete(
        long id,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await softDeleteUserUseCase.ExecuteAsync(
            new SoftDeleteUserCommand(id, CurrentUserId()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{id:long}/revoke-access")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> RevokeAccess(
        long id,
        RevokeUserAccessRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await revokeUserAccessUseCase.ExecuteAsync(
            RevokeUserAccessModel.FromRequest(id, CurrentUserId(), request),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{id:long}/restore-access")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> RestoreAccess(
        long id,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await restoreUserAccessUseCase.ExecuteAsync(
            new RestoreUserAccessCommand(id, CurrentUserId()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{id:long}/reset-signature-password")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> ResetSignaturePassword(
        long id,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await resetSignaturePasswordUseCase.ExecuteAsync(
            new ResetSignaturePasswordCommand(id, CurrentUserId()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{id:long}/modules")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> GrantModule(
        long id,
        GrantUserModuleRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await grantUserModuleUseCase.ExecuteAsync(
            new GrantUserModuleCommand(id, request.Module, CurrentUserId()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:long}/modules/{module}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserResponse>> RevokeModule(
        long id,
        string module,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        Result<UserResponse> result = await revokeUserModuleUseCase.ExecuteAsync(
            new RevokeUserModuleModel(id, module, CurrentUserId(), reason),
            cancellationToken);

        return ToActionResult(result);
    }
}
