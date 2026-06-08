using System.Reflection;
using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Tests.Application;

public sealed class StockMovementUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ManualOutput_ReducesMedicationStockAndCreatesMovement()
    {
        Medication medication = CreateMedication(1, 10);
        var movements = new FakeStockMovementRepository();
        var useCase = new CreateStockMovementUseCase(
            movements,
            new FakeMedicationRepository([medication]),
            new FakeUnitOfWork());

        Result<StockMovementResponse> result = await useCase.ExecuteAsync(
            Movement("saida", medication.Id, quantity: 4),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, medication.Quantity);
        Assert.Single(movements.Items);
        Assert.Equal("saida", movements.Items.Single().Type);
    }

    [Fact]
    public async Task ExecuteAsync_ManualOutputWithInsufficientStock_ReturnsValidationError()
    {
        Medication medication = CreateMedication(1, 3);
        var movements = new FakeStockMovementRepository();
        var useCase = new CreateStockMovementUseCase(
            movements,
            new FakeMedicationRepository([medication]),
            new FakeUnitOfWork());

        Result<StockMovementResponse> result = await useCase.ExecuteAsync(
            Movement("saida", medication.Id, quantity: 4),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Quantidade insuficiente em estoque.", result.Error!.Message);
        Assert.Equal(3, medication.Quantity);
        Assert.Empty(movements.Items);
    }

    [Fact]
    public async Task ExecuteAsync_EntryMovement_DoesNotChangeMedicationStock()
    {
        Medication medication = CreateMedication(1, 10);
        var movements = new FakeStockMovementRepository();
        var useCase = new CreateStockMovementUseCase(
            movements,
            new FakeMedicationRepository([medication]),
            new FakeUnitOfWork());

        Result<StockMovementResponse> result = await useCase.ExecuteAsync(
            Movement("entrada", medication.Id, quantity: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, medication.Quantity);
        Assert.Single(movements.Items);
    }

    private static StockMovementInputModel Movement(string type, long medicationId, int quantity)
    {
        return new StockMovementInputModel(
            type,
            medicationId,
            quantity,
            DateOnly.FromDateTime(DateTime.Today),
            "Responsavel",
            null,
            "L1",
            type == "saida" ? "Dispensacao" : "Entrada de estoque",
            null,
            null,
            null);
    }

    private static Medication CreateMedication(long id, int quantity)
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
            "Farmacia",
            1,
            5,
            false);

        SetId(medication, id);
        return medication;
    }

    private static void SetId(object entity, long id)
    {
        PropertyInfo property = entity.GetType().BaseType!.GetProperty("Id")!;
        property.SetValue(entity, id);
    }

    private sealed class FakeMedicationRepository(List<Medication> items) : IMedicationRepository
    {
        public Task<IReadOnlyList<Medication>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Medication>>(items);

        public Task<Medication?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task<bool> TryReduceQuantityAsync(long id, int quantity, CancellationToken cancellationToken)
        {
            Medication? medication = items.FirstOrDefault(item => item.Id == id);
            if (medication is null || medication.Quantity < quantity)
            {
                return Task.FromResult(false);
            }

            medication.ReduceQuantity(quantity);
            return Task.FromResult(true);
        }

        public Task AddAsync(Medication medication, CancellationToken cancellationToken)
        {
            items.Add(medication);
            return Task.CompletedTask;
        }

        public void Remove(Medication medication) => items.Remove(medication);

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
