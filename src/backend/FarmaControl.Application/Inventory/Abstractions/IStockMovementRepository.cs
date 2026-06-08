using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Inventory.Abstractions;

public interface IStockMovementRepository
{
    Task<IReadOnlyList<StockMovement>> ListAsync(CancellationToken cancellationToken);

    Task AddAsync(StockMovement movement, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
