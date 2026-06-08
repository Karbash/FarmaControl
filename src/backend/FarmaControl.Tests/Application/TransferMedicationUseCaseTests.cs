using System.Reflection;
using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Tests.Application;

public sealed class TransferMedicationUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_TotalTransfer_ChangesLocationAndCreatesMovement()
    {
        var medication = CreateMedication(1, 10, "Origem");
        var medications = new FakeMedicationRepository([medication]);
        var movements = new FakeStockMovementRepository();
        var locations = new FakeStockLocationRepository([CreateLocation(2, "Destino")]);
        var useCase = new TransferMedicationUseCase(medications, movements, locations, new FakeUnitOfWork());

        Result<TransferMedicationResponse> result = await useCase.ExecuteAsync(
            TransferRequest(1, 10, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Destino", medication.Location);
        Assert.Equal(2, medication.LocationId);
        Assert.Equal(10, medication.Quantity);
        Assert.Null(result.Value!.NewMedicationId);
        Assert.Single(movements.Items);
    }

    [Fact]
    public async Task ExecuteAsync_PartialTransfer_ReducesOriginalAndCreatesDestinationMedication()
    {
        var medication = CreateMedication(1, 10, "Origem");
        var medications = new FakeMedicationRepository([medication]);
        var movements = new FakeStockMovementRepository();
        var locations = new FakeStockLocationRepository([CreateLocation(2, "Destino")]);
        var useCase = new TransferMedicationUseCase(medications, movements, locations, new FakeUnitOfWork());

        Result<TransferMedicationResponse> result = await useCase.ExecuteAsync(
            TransferRequest(1, 4, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, medication.Quantity);
        Assert.Equal(2, medications.Items.Count);
        Assert.Equal(4, medications.Items[1].Quantity);
        Assert.Equal("Destino", medications.Items[1].Location);
        Assert.Equal(2, medications.Items[1].LocationId);
        Assert.Equal(2, result.Value!.NewMedicationId);
        Assert.Single(movements.Items);
    }

    [Fact]
    public async Task ExecuteAsync_InsufficientStock_ReturnsValidationError()
    {
        var medication = CreateMedication(1, 3, "Origem");
        var medications = new FakeMedicationRepository([medication]);
        var movements = new FakeStockMovementRepository();
        var locations = new FakeStockLocationRepository([CreateLocation(2, "Destino")]);
        var useCase = new TransferMedicationUseCase(medications, movements, locations, new FakeUnitOfWork());

        Result<TransferMedicationResponse> result = await useCase.ExecuteAsync(
            TransferRequest(1, 4, 2),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Quantidade insuficiente em estoque.", result.Error!.Message);
        Assert.Empty(movements.Items);
    }

    private static TransferMedicationModel TransferRequest(long medicationId, int quantity, long destinationLocationId)
    {
        return new TransferMedicationModel(
            medicationId,
            "Destino",
            destinationLocationId,
            quantity,
            "Responsavel",
            DateOnly.FromDateTime(DateTime.Today),
            null);
    }

    private static Medication CreateMedication(long id, int quantity, string location)
    {
        Medication medication = Medication.Create(
            "Dipirona",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Responsavel",
            null,
            null,
            "L1",
            null,
            quantity,
            "un",
            location,
            1,
            5,
            false);

        SetId(medication, id);
        return medication;
    }

    private static StockLocation CreateLocation(long id, string name)
    {
        StockLocation location = StockLocation.Create(name);
        SetId(location, id);
        return location;
    }

    private static void SetId(object entity, long id)
    {
        PropertyInfo property = entity.GetType().BaseType!.GetProperty("Id")!;
        property.SetValue(entity, id);
    }

    private sealed class FakeMedicationRepository(List<Medication> items) : IMedicationRepository
    {
        public List<Medication> Items { get; } = items;

        public Task<IReadOnlyList<Medication>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Medication>>(Items);

        public Task<Medication?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task<bool> TryReduceQuantityAsync(long id, int quantity, CancellationToken cancellationToken)
        {
            Medication? medication = Items.FirstOrDefault(item => item.Id == id);
            if (medication is null || medication.Quantity < quantity)
            {
                return Task.FromResult(false);
            }

            medication.ReduceQuantity(quantity);
            return Task.FromResult(true);
        }

        public Task AddAsync(Medication medication, CancellationToken cancellationToken)
        {
            SetId(medication, Items.Count + 1);
            Items.Add(medication);
            return Task.CompletedTask;
        }

        public void Remove(Medication medication) => Items.Remove(medication);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeStockMovementRepository : IStockMovementRepository
    {
        public List<StockMovement> Items { get; } = [];

        public Task<IReadOnlyList<StockMovement>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockMovement>>(Items);

        public Task AddAsync(StockMovement movement, CancellationToken cancellationToken)
        {
            SetId(movement, Items.Count + 1);
            Items.Add(movement);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeStockLocationRepository(List<StockLocation> items) : IStockLocationRepository
    {
        public Task<IReadOnlyList<StockLocation>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockLocation>>(items);

        public Task<StockLocation?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult(items.Any(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(StockLocation location, CancellationToken cancellationToken)
        {
            SetId(location, items.Count + 1);
            items.Add(location);
            return Task.CompletedTask;
        }

        public void Remove(StockLocation location) => items.Remove(location);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            return action(cancellationToken);
        }

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken)
        {
            return action(cancellationToken);
        }
    }
}
