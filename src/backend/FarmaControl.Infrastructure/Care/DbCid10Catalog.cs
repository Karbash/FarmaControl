using FarmaControl.Application.Care.Cid10;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Care;

public sealed class DbCid10Catalog(FarmaControlDbContext dbContext) : ICid10Catalog
{
    public async Task<IReadOnlyList<Cid10Response>> SearchAsync(
        string? query,
        CancellationToken cancellationToken)
    {
        string term = query?.Trim() ?? string.Empty;
        IQueryable<Cid10Code> result = dbContext.Cid10Codes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(term))
        {
            string pattern = $"%{term}%";
            result = result.Where(code =>
                EF.Functions.Like(code.Code, pattern) ||
                EF.Functions.Like(code.Name, pattern));
        }

        result = result
            .OrderBy(code => code.Code)
            .ThenBy(code => code.Name);

        if (!string.IsNullOrWhiteSpace(term))
        {
            result = result.Take(30);
        }

        return await result
            .Select(code => new Cid10Response(code.Id, code.Code, code.Name))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cid10Response>> GetByIdsAsync(
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        long[] distinctIds = ids
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        return await dbContext.Cid10Codes
            .AsNoTracking()
            .Where(code => distinctIds.Contains(code.Id))
            .Select(code => new Cid10Response(code.Id, code.Code, code.Name))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Cid10Response?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        string normalized = code.Trim();

        return await dbContext.Cid10Codes
            .AsNoTracking()
            .Where(item => item.Code == normalized)
            .OrderBy(item => item.Code)
            .ThenBy(item => item.Name)
            .Select(item => new Cid10Response(item.Id, item.Code, item.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
