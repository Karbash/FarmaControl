using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record GrantUserModuleCommand(long UserId, string Module, long GrantedByUserId);

public sealed class GrantUserModuleUseCase(
    IUserRepository users,
    IAuditLogger auditLogger)
    : IUseCase<GrantUserModuleCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        GrantUserModuleCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.GrantedByUserId <= 0 || string.IsNullOrWhiteSpace(request.Module))
        {
            return Result<UserResponse>.Failure(AppError.Validation("Dados de modulo invalidos."));
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(AppError.NotFound("Usuario nao encontrado."));
        }

        user.GrantModule(request.Module, request.GrantedByUserId);
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.GrantedByUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "editar",
            "usuario",
            user.Id,
            $"Concedeu modulo \"{request.Module}\" ao usuario \"{user.Name}\" ({user.Email})",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}

public sealed class RevokeUserModuleUseCase(
    IUserRepository users,
    IAuditLogger auditLogger)
    : IUseCase<RevokeUserModuleModel, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        RevokeUserModuleModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<UserResponse>.Failure(errors.FirstOrDefaultError());
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(AppError.NotFound("Usuario nao encontrado."));
        }

        user.RevokeModule(request.Module, request.RevokedByUserId, request.Reason);
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.RevokedByUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "editar",
            "usuario",
            user.Id,
            $"Revogou modulo \"{request.Module}\" do usuario \"{user.Name}\" ({user.Email})",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
