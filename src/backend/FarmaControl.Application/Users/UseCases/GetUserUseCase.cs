using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record GetUserRequest(long UserId);

public sealed class GetUserUseCase(IUserRepository users)
    : IUseCase<GetUserRequest, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> ExecuteAsync(
        GetUserRequest request,
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

        return Result<UserResponse>.Success(UserModel.FromDomain(user));
    }
}
