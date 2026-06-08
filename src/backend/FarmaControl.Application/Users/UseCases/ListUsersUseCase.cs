using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Application.Users.Models;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record ListUsersRequest(bool IncludeDeleted);

public sealed class ListUsersUseCase(IUserRepository users)
    : IUseCase<ListUsersRequest, IReadOnlyList<UserResponse>>
{
    public async Task<IReadOnlyList<UserResponse>> ExecuteAsync(
        ListUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await users.ListAsync(request.IncludeDeleted, cancellationToken);
        return result.Select(UserModel.FromDomain).ToArray();
    }
}
