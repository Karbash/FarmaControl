using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record UpdateUserCommand(long UserId, long ActorUserId, UpdateUserModel Model);

public sealed class UpdateUserUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAuditLogger auditLogger)
    : IUseCase<UpdateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            return Result<UserResponse>.Failure(AppError.Validation("Usuario e obrigatorio."));
        }

        IReadOnlyList<AppError> errors = request.Model.Validate();
        if (errors.HasErrors())
        {
            return Result<UserResponse>.Failure(errors.FirstOrDefaultError());
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(AppError.NotFound("Usuario nao encontrado."));
        }

        if (await users.EmailExistsAsync(request.Model.Email, request.UserId, cancellationToken))
        {
            return Result<UserResponse>.Failure(AppError.Conflict("Email ja cadastrado."));
        }

        string? passwordHash = string.IsNullOrWhiteSpace(request.Model.Password)
            ? null
            : passwordHasher.Hash(request.Model.Password);

        request.Model.ApplyTo(user, passwordHash);
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.ActorUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "editar",
            "usuario",
            user.Id,
            $"Editou usuario \"{user.Name}\" ({user.Email}) - role: {user.Role.Value}",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
