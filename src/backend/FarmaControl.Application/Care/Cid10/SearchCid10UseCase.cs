using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;

namespace FarmaControl.Application.Care.Cid10;

public sealed record SearchCid10Request(string? Query);

public sealed class SearchCid10UseCase(ICid10Catalog catalog)
    : IUseCase<SearchCid10Request, IReadOnlyList<Cid10Response>>
{
    public Task<IReadOnlyList<Cid10Response>> ExecuteAsync(
        SearchCid10Request request,
        CancellationToken cancellationToken)
    {
        return catalog.SearchAsync(request.Query, cancellationToken);
    }
}
