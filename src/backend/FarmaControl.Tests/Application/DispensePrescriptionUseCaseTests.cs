using System.Reflection;
using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Care.MedicalAttendances.Abstractions;
using FarmaControl.Application.Care.MedicalRecords.Abstractions;
using FarmaControl.Application.Care.Prescriptions.Abstractions;
using FarmaControl.Application.Care.Prescriptions.Models;
using FarmaControl.Application.Care.Prescriptions.UseCases;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Tests.Application;

public sealed class DispensePrescriptionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithStock_ReducesStockCreatesMovementAndMarksDispensed()
    {
        Prescription prescription = CreatePrescription(1, medicationId: 10, quantity: 2);
        Medication medication = CreateMedication(10, quantity: 5);
        var prescriptions = new FakePrescriptionRepository([prescription]);
        var medications = new FakeMedicationRepository([medication]);
        var movements = new FakeStockMovementRepository();
        MedicalAttendance attendance = CreateAttendance(1);
        var useCase = new DispensePrescriptionUseCase(
            prescriptions,
            medications,
            movements,
            new FakeMedicalRecordRepository([CreateMedicalRecord(1, appointmentId: 1)]),
            new FakeMedicalAttendanceRepository([attendance]),
            new FakeUnitOfWork());

        Result<DispensePrescriptionResponse> result = await useCase.ExecuteAsync(
            new DispensePrescriptionModel(1, "Farmaceutico", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, medication.Quantity);
        Assert.True(prescription.IsDispensed);
        Assert.Single(movements.Items);
        Assert.Single(attendance.Dispensations);
        Assert.Equal(prescription.Id, attendance.Dispensations.Single().PrescriptionId);
        Assert.Equal("L1", result.Value!.Batch);
    }

    [Fact]
    public async Task ExecuteAsync_SelectsLotByNearestExpirationDate()
    {
        Prescription prescription = CreatePrescription(1, medicationId: 10, quantity: 2);
        Medication laterLot = CreateMedication(10, quantity: 10, batch: "LOTE-LONGO", expirationDate: new DateOnly(2099, 12, 31));
        Medication earlierLot = CreateMedication(11, quantity: 10, batch: "LOTE-CURTO", expirationDate: new DateOnly(2099, 7, 10));
        var movements = new FakeStockMovementRepository();
        MedicalAttendance attendance = CreateAttendance(1);
        var useCase = new DispensePrescriptionUseCase(
            new FakePrescriptionRepository([prescription]),
            new FakeMedicationRepository([laterLot, earlierLot]),
            movements,
            new FakeMedicalRecordRepository([CreateMedicalRecord(1, appointmentId: 1)]),
            new FakeMedicalAttendanceRepository([attendance]),
            new FakeUnitOfWork());

        Result<DispensePrescriptionResponse> result = await useCase.ExecuteAsync(
            new DispensePrescriptionModel(1, "Farmaceutico", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(11, result.Value!.MedicationId);
        Assert.Equal("LOTE-CURTO", result.Value.Batch);
        Assert.Equal(8, earlierLot.Quantity);
        Assert.Equal(10, laterLot.Quantity);
        Assert.Equal("LOTE-CURTO", movements.Items.Single().Batch);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutUnifiedAttendance_StillReducesStockAndMarksDispensed()
    {
        Prescription prescription = CreatePrescription(1, medicationId: 10, quantity: 2);
        Medication medication = CreateMedication(10, quantity: 5);
        var movements = new FakeStockMovementRepository();
        var useCase = new DispensePrescriptionUseCase(
            new FakePrescriptionRepository([prescription]),
            new FakeMedicationRepository([medication]),
            movements,
            new FakeMedicalRecordRepository([CreateMedicalRecord(1, appointmentId: 1)]),
            new FakeMedicalAttendanceRepository([]),
            new FakeUnitOfWork());

        Result<DispensePrescriptionResponse> result = await useCase.ExecuteAsync(
            new DispensePrescriptionModel(1, "Farmaceutico", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, medication.Quantity);
        Assert.True(prescription.IsDispensed);
        Assert.Single(movements.Items);
        Assert.Null(movements.Items.Single().AttendanceId);
        Assert.Null(result.Value!.MedicalAttendanceId);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateDispense_ReturnsValidationError()
    {
        Prescription prescription = CreatePrescription(1, medicationId: 10, quantity: 1);
        prescription.MarkDispensed();
        var useCase = new DispensePrescriptionUseCase(
            new FakePrescriptionRepository([prescription]),
            new FakeMedicationRepository([CreateMedication(10, 5)]),
            new FakeStockMovementRepository(),
            new FakeMedicalRecordRepository([CreateMedicalRecord(1, appointmentId: 1)]),
            new FakeMedicalAttendanceRepository([CreateAttendance(1)]),
            new FakeUnitOfWork());

        Result<DispensePrescriptionResponse> result = await useCase.ExecuteAsync(
            new DispensePrescriptionModel(1, "Farmaceutico", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Prescricao ja dispensada.", result.Error!.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutMedicationLink_ReturnsValidationError()
    {
        Prescription prescription = CreatePrescription(1, medicationId: null, quantity: 1);
        var useCase = new DispensePrescriptionUseCase(
            new FakePrescriptionRepository([prescription]),
            new FakeMedicationRepository([]),
            new FakeStockMovementRepository(),
            new FakeMedicalRecordRepository([CreateMedicalRecord(1, appointmentId: 1)]),
            new FakeMedicalAttendanceRepository([CreateAttendance(1)]),
            new FakeUnitOfWork());

        Result<DispensePrescriptionResponse> result = await useCase.ExecuteAsync(
            new DispensePrescriptionModel(1, "Farmaceutico", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Medicamento nao vinculado ao estoque.", result.Error!.Message);
    }

    [Fact]
    public async Task ExecuteAsync_InsufficientStock_ReturnsValidationError()
    {
        Prescription prescription = CreatePrescription(1, medicationId: 10, quantity: 6);
        var useCase = new DispensePrescriptionUseCase(
            new FakePrescriptionRepository([prescription]),
            new FakeMedicationRepository([CreateMedication(10, 5)]),
            new FakeStockMovementRepository(),
            new FakeMedicalRecordRepository([CreateMedicalRecord(1, appointmentId: 1)]),
            new FakeMedicalAttendanceRepository([CreateAttendance(1)]),
            new FakeUnitOfWork());

        Result<DispensePrescriptionResponse> result = await useCase.ExecuteAsync(
            new DispensePrescriptionModel(1, "Farmaceutico", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Nenhum lote valido possui estoque suficiente para dispensacao.", result.Error!.Message);
    }

    private static Prescription CreatePrescription(long id, long? medicationId, int quantity)
    {
        Prescription prescription = Prescription.Create(1, 1, medicationId, "Dipirona", "500mg", "Tomar", quantity, null);
        SetId(prescription, id);
        return prescription;
    }

    private static Medication CreateMedication(
        long id,
        int quantity,
        string batch = "L1",
        DateOnly? expirationDate = null)
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
            null,
            null,
            null,
            batch,
            expirationDate,
            quantity,
            "un",
            "Farmacia",
            1,
            5,
            false);
        SetId(medication, id);
        return medication;
    }

    private static MedicalRecord CreateMedicalRecord(long id, long appointmentId)
    {
        MedicalRecord record = MedicalRecord.Create(appointmentId, 1, "Medico", null, null, null, null, null, null);
        SetId(record, id);
        return record;
    }

    private static MedicalAttendance CreateAttendance(long appointmentId)
    {
        MedicalAttendance attendance = MedicalAttendance.Create(
            appointmentId,
            1,
            null,
            "Responsavel",
            "Paciente",
            30,
            DateOnly.FromDateTime(DateTime.Today),
            null,
            null,
            null,
            null,
            AttendanceType.Participante,
            null);

        SetId(attendance, appointmentId);
        return attendance;
    }

    private static void SetId(object entity, long id)
    {
        PropertyInfo property = entity.GetType().BaseType!.GetProperty("Id")!;
        property.SetValue(entity, id);
    }

    private sealed class FakePrescriptionRepository(List<Prescription> items) : IPrescriptionRepository
    {
        public List<Prescription> Items { get; } = items;

        public Task<IReadOnlyList<Prescription>> ListAsync(long? medicalRecordId, long? patientId, bool? isDispensed, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Prescription>>(Items);

        public Task<Prescription?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(item => item.Id == id));

        public Task AddAsync(Prescription prescription, CancellationToken cancellationToken)
        {
            Items.Add(prescription);
            return Task.CompletedTask;
        }

        public void Remove(Prescription prescription) => Items.Remove(prescription);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMedicalRecordRepository(List<MedicalRecord> items) : IMedicalRecordRepository
    {
        public Task<IReadOnlyList<MedicalRecord>> ListAsync(long? appointmentId, long? patientId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MedicalRecord>>(items);

        public Task<MedicalRecord?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task AddAsync(MedicalRecord record, CancellationToken cancellationToken)
        {
            items.Add(record);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeMedicalAttendanceRepository(List<MedicalAttendance> items) : IMedicalAttendanceRepository
    {
        public Task<MedicalAttendance?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.Id == id));

        public Task<MedicalAttendance?> GetByAppointmentIdAsync(long appointmentId, CancellationToken cancellationToken) =>
            Task.FromResult(items.FirstOrDefault(item => item.AppointmentId == appointmentId));

        public Task<bool> ExistsForAppointmentAsync(long appointmentId, CancellationToken cancellationToken) =>
            Task.FromResult(items.Any(item => item.AppointmentId == appointmentId));

        public Task AddAsync(MedicalAttendance attendance, CancellationToken cancellationToken)
        {
            items.Add(attendance);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken) =>
            action(cancellationToken);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) =>
            action(cancellationToken);
    }
}
