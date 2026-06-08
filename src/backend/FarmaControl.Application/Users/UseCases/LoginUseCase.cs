using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Auth;

namespace FarmaControl.Application.Users.UseCases;

public sealed class LoginUseCase(
    IUserRepository users,
    IPasswordHasher passwordHasher)
    : IUseCase<LoginModel, Result<AuthenticatedUserResponse>>
{
    public async Task<Result<AuthenticatedUserResponse>> ExecuteAsync(
        LoginModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<AuthenticatedUserResponse>.Failure(errors.FirstOrDefaultError());
        }

        var user = await users.GetByEmailAsync(request.NormalizedEmail(), cancellationToken);
        if (user is null || !user.CanAuthenticate || !passwordHasher.Verify(user, request.Password))
        {
            return Result<AuthenticatedUserResponse>.Failure(
                AppError.Forbidden("Email ou senha incorretos."));
        }

        return Result<AuthenticatedUserResponse>.Success(UserModel.ToAuthenticatedResponse(user));
    }
}
