using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Domain.Inventory;
using FarmaControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FarmaControl.Infrastructure.Inventory;

public sealed class EfMedicationRepository(FarmaControlDbContext dbContext) : IMedicationRepository
{
    public async Task<IReadOnlyList<Medication>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Medications
            .AsNoTracking()
            .OrderBy(medication => medication.GenericName)
            .ThenBy(medication => medication.CommercialName)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Medication?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext.Medications.FirstOrDefaultAsync(
            medication => medication.Id == id,
            cancellationToken);
    }

    public async Task<bool> TryReduceQuantityAsync(long id, int quantity, CancellationToken cancellationToken)
    {
        int updatedRows = await dbContext.Medications
            .Where(medication => medication.Id == id && medication.Quantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(medication => medication.Quantity, medication => medication.Quantity - quantity)
                    .SetProperty(medication => medication.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken);

        return updatedRows == 1;
    }

    public async Task AddAsync(Medication medication, CancellationToken cancellationToken)
    {
        await dbContext.Medications.AddAsync(medication, cancellationToken);
    }

    public void Remove(Medication medication)
    {
        dbContext.Medications.Remove(medication);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
