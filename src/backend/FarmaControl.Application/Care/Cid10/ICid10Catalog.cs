using FarmaControl.Contracts.Care;

namespace FarmaControl.Application.Care.Cid10;

public interface ICid10Catalog
{
    Task<IReadOnlyList<Cid10Response>> SearchAsync(string? query, CancellationToken cancellationToken);

    Task<IReadOnlyList<Cid10Response>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken);

    Task<Cid10Response?> GetByCodeAsync(string code, CancellationToken cancellationToken);
}
