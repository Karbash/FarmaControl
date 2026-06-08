using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Auth;

namespace FarmaControl.Application.Users.UseCases;

public sealed record GetCurrentUserRequest(long UserId);

public sealed class GetCurrentUserUseCase(IUserRepository users)
    : IUseCase<GetCurrentUserRequest, Result<AuthenticatedUserResponse>>
{
    public async Task<Result<AuthenticatedUserResponse>> ExecuteAsync(
        GetCurrentUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
        {
            return Result<AuthenticatedUserResponse>.Failure(
                AppError.Forbidden("Usuario nao autenticado."));
        }

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.CanAuthenticate)
        {
            return Result<AuthenticatedUserResponse>.Failure(
                AppError.Forbidden("Usuario nao autenticado."));
        }

        return Result<AuthenticatedUserResponse>.Success(UserModel.ToAuthenticatedResponse(user));
    }
}
