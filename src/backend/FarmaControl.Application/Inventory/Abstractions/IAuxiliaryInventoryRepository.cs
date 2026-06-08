using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Inventory.Abstractions;

public interface IDonorRepository
{
    Task<IReadOnlyList<Donor>> ListAsync(CancellationToken cancellationToken);

    Task<Donor?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task AddAsync(Donor donor, CancellationToken cancellationToken);

    void Remove(Donor donor);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IManufacturerRepository
{
    Task<IReadOnlyList<Manufacturer>> ListAsync(CancellationToken cancellationToken);

    Task<Manufacturer?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task AddAsync(Manufacturer manufacturer, CancellationToken cancellationToken);

    void Remove(Manufacturer manufacturer);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IStockLocationRepository
{
    Task<IReadOnlyList<StockLocation>> ListAsync(CancellationToken cancellationToken);

    Task<StockLocation?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(StockLocation location, CancellationToken cancellationToken);

    void Remove(StockLocation location);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
