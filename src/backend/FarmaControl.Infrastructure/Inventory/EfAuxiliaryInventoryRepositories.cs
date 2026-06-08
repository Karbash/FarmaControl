using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Domain.Inventory;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Inventory;

public sealed class EfDonorRepository(FarmaControlDbContext dbContext) : IDonorRepository
{
    public async Task<IReadOnlyList<Donor>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Donors
            .AsNoTracking()
            .OrderBy(donor => donor.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Donor?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.Donors.FirstOrDefaultAsync(donor => donor.Id == id, cancellationToken);
    }

    public async Task AddAsync(Donor donor, CancellationToken cancellationToken)
    {
        await dbContext.Donors.AddAsync(donor, cancellationToken);
    }

    public void Remove(Donor donor)
    {
        dbContext.Donors.Remove(donor);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EfManufacturerRepository(FarmaControlDbContext dbContext) : IManufacturerRepository
{
    public async Task<IReadOnlyList<Manufacturer>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Manufacturers
            .AsNoTracking()
            .OrderBy(manufacturer => manufacturer.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Manufacturer?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.Manufacturers.FirstOrDefaultAsync(
            manufacturer => manufacturer.Id == id,
            cancellationToken);
    }

    public async Task AddAsync(Manufacturer manufacturer, CancellationToken cancellationToken)
    {
        await dbContext.Manufacturers.AddAsync(manufacturer, cancellationToken);
    }

    public void Remove(Manufacturer manufacturer)
    {
        dbContext.Manufacturers.Remove(manufacturer);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class EfStockLocationRepository(FarmaControlDbContext dbContext) : IStockLocationRepository
{
    public async Task<IReadOnlyList<StockLocation>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.StockLocations
            .AsNoTracking()
            .OrderBy(location => location.Name)
            .ToArrayAsync(cancellationToken);
    }

    public Task<StockLocation?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.StockLocations.FirstOrDefaultAsync(
            location => location.Id == id,
            cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
    {
        string normalized = name.Trim();
        return dbContext.StockLocations.AnyAsync(
            location => location.Name == normalized,
            cancellationToken);
    }

    public async Task AddAsync(StockLocation location, CancellationToken cancellationToken)
    {
        await dbContext.StockLocations.AddAsync(location, cancellationToken);
    }

    public void Remove(StockLocation location)
    {
        dbContext.StockLocations.Remove(location);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
