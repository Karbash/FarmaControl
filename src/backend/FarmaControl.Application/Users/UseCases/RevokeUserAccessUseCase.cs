using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed class RevokeUserAccessUseCase(
    IUserRepository users,
    IAuditLogger auditLogger)
    : IUseCase<RevokeUserAccessModel, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        RevokeUserAccessModel request,
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

        user.RevokeAccess(request.RevokedByUserId, request.Reason);
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.RevokedByUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "editar",
            "usuario",
            user.Id,
            $"Revogou acesso do usuario \"{user.Name}\" ({user.Email})",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
