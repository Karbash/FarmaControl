using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.UseCases;

public sealed record CareTeamUserResponse(long Id, string Name, string Role);

public sealed class ListCareTeamUseCase(IUserRepository users)
    : IUseCase<NoRequest, IReadOnlyList<CareTeamUserResponse>>
{
    public async Task<IReadOnlyList<CareTeamUserResponse>> ExecuteAsync(
        NoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await users.ListCareTeamAsync(cancellationToken);
        return result
            .Select(user => new CareTeamUserResponse(user.Id, user.Name, user.Role.Value))
            .ToArray();
    }
}

public sealed class ListResponsibleUsersUseCase(IUserRepository users)
    : IUseCase<ListResponsibleUsersRequest, IReadOnlyList<ResponsibleUserResponse>>
{
    public async Task<IReadOnlyList<ResponsibleUserResponse>> ExecuteAsync(
        ListResponsibleUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await users.ListResponsibleUsersAsync(cancellationToken);
        return result
            .Select(user => new ResponsibleUserResponse(user.Id, user.Name, user.Role.Value))
            .ToArray();
    }
}

public sealed record ListResponsibleUsersRequest;
