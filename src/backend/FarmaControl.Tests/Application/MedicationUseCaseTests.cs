using System.Reflection;
using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Inventory.UseCases;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;
using FarmaControl.Domain.Users;

namespace FarmaControl.Tests.Application;

public sealed class MedicationUseCaseTests
{
    [Fact]
    public async Task Create_WithValidSignature_CreatesMedication()
    {
        User actor = CreateUser();
        var medications = new FakeMedicationRepository();
        var useCase = CreateUseCase(medications, actor);

        Result<MedicationResponse> result = await useCase.ExecuteAsync(
            new CreateMedicationCommand(actor.Id, ValidMedicationModel(), "assinatura"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(medications.Items);
        Assert.Equal("Dipirona", result.Value!.GenericName);
    }

    [Fact]
    public async Task Create_WithWrongSignature_DoesNotCreateMedication()
    {
        User actor = CreateUser();
        var medications = new FakeMedicationRepository();
        var useCase = CreateUseCase(medications, actor);

        Result<MedicationResponse> result = await useCase.ExecuteAsync(
            new CreateMedicationCommand(actor.Id, ValidMedicationModel(), "errada"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Senha de assinatura incorreta.", result.Error!.Message);
        Assert.Empty(medications.Items);
    }

    private static CreateMedicationUseCase CreateUseCase(
        FakeMedicationRepository medications,
        User actor)
    {
        return new CreateMedicationUseCase(
            medications,
            new FakeDonorRepository([]),
            new FakeManufacturerRepository([]),
            new FakeStockLocationRepository([CreateLocation(1, "Farmacia")]),
            new FakeUserRepository(actor),
            new FakePasswordHasher());
    }

    private static MedicationInputModel ValidMedicationModel()
    {
        return new MedicationInputModel(
            "Dipirona",
            null,
            "Analgesico",
            "Comprimido",
            "500mg",
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            "Responsavel",
            null,
            null,
            "L1",
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            10,
            "un",
            "Farmacia",
            1,
            5,
            false);
    }

    private static User CreateUser()
    {
        User user = User.Create("Profissional", "profissional@teste", "hash", UserRole.Entrada);
        user.ChangeSignaturePasswordHash("assinatura");
        SetId(user, 7);
        return user;
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

    private sealed class FakeMedicationRepository : IMedicationRepository
    {
        public List<Medication> Items { get; } = [];

        public Task<IReadOnlyList<Medication>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Medication>>(Items);

        public Task<Medication?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task<bool> TryReduceQuantityAsync(long id, int quantity, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(Medication medication, CancellationToken cancellationToken)
        {
            SetId(medication, Items.Count + 1);
            Items.Add(medication);
            return Task.CompletedTask;
        }

        public void Remove(Medication medication) => Items.Remove(medication);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDonorRepository(List<Donor> items) : IDonorRepository
    {
        public Task<IReadOnlyList<Donor>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Donor>>(items);

        public Task<Donor?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task AddAsync(Donor donor, CancellationToken cancellationToken) => Task.CompletedTask;

        public void Remove(Donor donor) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeManufacturerRepository(List<Manufacturer> items) : IManufacturerRepository
    {
        public Task<IReadOnlyList<Manufacturer>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Manufacturer>>(items);

        public Task<Manufacturer?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task AddAsync(Manufacturer manufacturer, CancellationToken cancellationToken) => Task.CompletedTask;

        public void Remove(Manufacturer manufacturer) { }

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

        public Task AddAsync(StockLocation location, CancellationToken cancellationToken) => Task.CompletedTask;

        public void Remove(StockLocation location) { }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id == id ? user : null);

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(null);

        public Task<IReadOnlyList<User>> ListAsync(bool includeDeleted, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([user]);

        public Task<IReadOnlyList<User>> ListCareTeamAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([user]);

        public Task<IReadOnlyList<User>> ListResponsibleUsersAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([user]);

        public Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task AddAsync(User item, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(User user, string password) => user.PasswordHash == password;

        public bool VerifyHash(string passwordHash, string password) => passwordHash == password;
    }
}
