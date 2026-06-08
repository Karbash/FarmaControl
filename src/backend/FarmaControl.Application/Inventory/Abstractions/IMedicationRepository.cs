using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Inventory.Abstractions;

public interface IMedicationRepository
{
    Task<IReadOnlyList<Medication>> ListAsync(CancellationToken cancellationToken);

    Task<Medication?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> TryReduceQuantityAsync(long id, int quantity, CancellationToken cancellationToken);

    Task AddAsync(Medication medication, CancellationToken cancellationToken);

    void Remove(Medication medication);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
