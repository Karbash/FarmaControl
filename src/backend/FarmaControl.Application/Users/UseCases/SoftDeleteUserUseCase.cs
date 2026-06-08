using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record SoftDeleteUserCommand(long UserId, long DeletedByUserId);

public sealed class SoftDeleteUserUseCase(
    IUserRepository users,
    IAuditLogger auditLogger)
    : IUseCase<SoftDeleteUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        SoftDeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0 || request.DeletedByUserId <= 0)
        {
            return Result<UserResponse>.Failure(AppError.Validation("Usuario e obrigatorio."));
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(AppError.NotFound("Usuario nao encontrado."));
        }

        user.SoftDelete(request.DeletedByUserId);
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.DeletedByUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "excluir",
            "usuario",
            user.Id,
            $"Aplicou soft delete no usuario \"{user.Name}\" ({user.Email})",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
