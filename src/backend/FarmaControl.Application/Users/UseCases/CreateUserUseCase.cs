using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Audit.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record CreateUserCommand(long ActorUserId, CreateUserModel Model);

public sealed class CreateUserUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IAuditLogger auditLogger)
    : IUseCase<CreateUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Model.Validate();
        if (errors.HasErrors())
        {
            return Result<UserResponse>.Failure(errors.FirstOrDefaultError());
        }

        if (await users.EmailExistsAsync(request.Model.Email, null, cancellationToken))
        {
            return Result<UserResponse>.Failure(AppError.Conflict("Email ja cadastrado."));
        }

        var user = request.Model.ToDomain(passwordHasher.Hash(request.Model.Password));
        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        var actor = await users.GetByIdAsync(request.ActorUserId, cancellationToken);
        await auditLogger.LogAsync(
            actor?.Id,
            actor?.Name,
            "criar",
            "usuario",
            user.Id,
            $"Criou usuario \"{user.Name}\" ({user.Email}) - role: {user.Role.Value}",
            cancellationToken);

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
