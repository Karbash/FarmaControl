using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record RestoreUserAccessCommand(long UserId, long ActorUserId);

public sealed class RestoreUserAccessUseCase(
    IUserRepository users,
    IAuditLogger auditLogger)
    : IUseCase<RestoreUserAccessCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        RestoreUserAccessCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            return Result<UserResponse>.Failure(AppError.Validation("Usuario e obrigatorio."));
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(AppError.NotFound("Usuario nao encontrado."));
        }

        user.RestoreAccess();
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.ActorUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "editar",
            "usuario",
            user.Id,
            $"Restaurou acesso do usuario \"{user.Name}\" ({user.Email})",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
