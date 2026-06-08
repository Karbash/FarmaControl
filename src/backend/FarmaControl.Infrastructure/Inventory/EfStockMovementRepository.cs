using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Domain.Inventory;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Inventory;

public sealed class EfStockMovementRepository(FarmaControlDbContext dbContext) : IStockMovementRepository
{
    public async Task<IReadOnlyList<StockMovement>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.StockMovements
            .AsNoTracking()
            .OrderByDescending(movement => movement.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AddAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        await dbContext.StockMovements.AddAsync(movement, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
