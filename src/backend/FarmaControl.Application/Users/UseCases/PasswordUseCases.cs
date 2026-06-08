using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Auth;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed class ChangePasswordUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher)
    : IUseCase<ChangePasswordModel, Result<AuthenticatedUserResponse>>
{
    public async Task<Result<AuthenticatedUserResponse>> ExecuteAsync(
        ChangePasswordModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<AuthenticatedUserResponse>.Failure(errors.FirstOrDefaultError());
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.CanAuthenticate)
        {
            return Result<AuthenticatedUserResponse>.Failure(AppError.Forbidden("Usuario nao autenticado."));
        }

        if (!passwordHasher.Verify(user, request.CurrentPassword))
        {
            return Result<AuthenticatedUserResponse>.Failure(AppError.Forbidden("Senha atual incorreta."));
        }

        user.ChangePasswordHash(passwordHasher.Hash(request.NewPassword));
        await users.SaveChangesAsync(cancellationToken);

        return Result<AuthenticatedUserResponse>.Success(UserModel.ToAuthenticatedResponse(user));
    }
}

public sealed class ChangeSignaturePasswordUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher)
    : IUseCase<ChangeSignaturePasswordModel, Result<AuthenticatedUserResponse>>
{
    public async Task<Result<AuthenticatedUserResponse>> ExecuteAsync(
        ChangeSignaturePasswordModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<AuthenticatedUserResponse>.Failure(errors.FirstOrDefaultError());
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.CanAuthenticate)
        {
            return Result<AuthenticatedUserResponse>.Failure(AppError.Forbidden("Usuario nao autenticado."));
        }

        bool canChange = user.SignaturePasswordResetRequired
            ? passwordHasher.Verify(user, request.CurrentPassword ?? string.Empty)
            : !string.IsNullOrWhiteSpace(user.SignaturePasswordHash) &&
                passwordHasher.VerifyHash(
                    user.SignaturePasswordHash,
                    request.CurrentSignaturePassword ?? string.Empty);

        if (!canChange)
        {
            return Result<AuthenticatedUserResponse>.Failure(AppError.Forbidden("Credencial atual incorreta."));
        }

        user.ChangeSignaturePasswordHash(passwordHasher.Hash(request.NewSignaturePassword));
        await users.SaveChangesAsync(cancellationToken);

        return Result<AuthenticatedUserResponse>.Success(UserModel.ToAuthenticatedResponse(user));
    }
}

public sealed record ResetSignaturePasswordCommand(long UserId, long ActorUserId);

public sealed class ResetSignaturePasswordUseCase(
    IUserRepository users,
    IAuditLogger auditLogger)
    : IUseCase<ResetSignaturePasswordCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        ResetSignaturePasswordCommand request,
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

        user.ResetSignaturePassword();
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.ActorUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "editar",
            "usuario",
            user.Id,
            $"Resetou senha de assinatura do usuario \"{user.Name}\" ({user.Email})",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
