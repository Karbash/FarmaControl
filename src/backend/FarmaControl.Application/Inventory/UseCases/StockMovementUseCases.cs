using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Inventory.UseCases;

public sealed class ListStockMovementsUseCase(IStockMovementRepository movements)
    : IUseCase<NoRequest, IReadOnlyList<StockMovementResponse>>
{
    public async Task<IReadOnlyList<StockMovementResponse>> ExecuteAsync(
        NoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await movements.ListAsync(cancellationToken);
        return result.Select(StockMovementModel.FromDomain).ToArray();
    }
}

public sealed class CreateStockMovementUseCase(
    IStockMovementRepository movements,
    IMedicationRepository medications,
    IUnitOfWork unitOfWork)
    : IUseCase<StockMovementInputModel, Result<StockMovementResponse>>
{
    public async Task<Result<StockMovementResponse>> ExecuteAsync(
        StockMovementInputModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<StockMovementResponse>.Failure(errors.FirstOrDefaultError());
        }

        Medication? medication = await medications.GetByIdAsync(request.MedicationId, cancellationToken);
        if (medication is null)
        {
            return Result<StockMovementResponse>.Failure(AppError.NotFound("Medicamento nao encontrado."));
        }

        if (IsManualOutput(request) && request.Quantity > medication.Quantity)
        {
            return Result<StockMovementResponse>.Failure(
                AppError.Validation("Quantidade insuficiente em estoque."));
        }

        Result<StockMovement> movementResult = await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                if (IsManualOutput(request))
                {
                    bool reduced = await medications.TryReduceQuantityAsync(
                        medication.Id,
                        request.Quantity,
                        ct);
                    if (!reduced)
                    {
                        return Result<StockMovement>.Failure(
                            AppError.Validation("Quantidade insuficiente em estoque."));
                    }
                }

                StockMovement movement = request.ToDomain();
                await movements.AddAsync(movement, ct);

                return Result<StockMovement>.Success(movement);
            },
            cancellationToken);

        return !movementResult.IsSuccess || movementResult.Value is null
            ? Result<StockMovementResponse>.Failure(movementResult.Error!)
            : Result<StockMovementResponse>.Success(StockMovementModel.FromDomain(movementResult.Value));
    }

    private static bool IsManualOutput(StockMovementInputModel request)
    {
        return string.Equals(request.Type.Trim(), "saida", StringComparison.OrdinalIgnoreCase) &&
            request.AttendanceId is null &&
            request.AppointmentId is null &&
            request.PrescriptionId is null;
    }
}

public sealed class TransferMedicationUseCase(
    IMedicationRepository medications,
    IStockMovementRepository movements,
    IStockLocationRepository locations,
    IUnitOfWork unitOfWork)
    : IUseCase<TransferMedicationModel, Result<TransferMedicationResponse>>
{
    public async Task<Result<TransferMedicationResponse>> ExecuteAsync(
        TransferMedicationModel request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<TransferMedicationResponse>.Failure(errors.FirstOrDefaultError());
        }

        Medication? medication = await medications.GetByIdAsync(request.MedicationId, cancellationToken);
        if (medication is null)
        {
            return Result<TransferMedicationResponse>.Failure(AppError.NotFound("Medicamento nao encontrado."));
        }

        if (request.Quantity > medication.Quantity)
        {
            return Result<TransferMedicationResponse>.Failure(
                AppError.Validation("Quantidade insuficiente em estoque."));
        }

        StockLocation? destinationLocation = request.DestinationLocationId.HasValue
            ? await locations.GetByIdAsync(request.DestinationLocationId.Value, cancellationToken)
            : null;
        if (destinationLocation is null)
        {
            return Result<TransferMedicationResponse>.Failure(
                AppError.Validation("Local de destino nao encontrado."));
        }

        long? newMedicationId = null;
        StockMovement? movement = null;

        Result<bool> transferResult = await unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                string originLocation = string.IsNullOrWhiteSpace(medication.Location)
                    ? "-"
                    : medication.Location;

                string notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? $"{originLocation} -> {destinationLocation.Name}"
                    : $"{originLocation} -> {destinationLocation.Name} | {request.Notes}";

                if (request.Quantity == medication.Quantity)
                {
                    medication.ChangeLocation(destinationLocation.Name, destinationLocation.Id);
                }
                else
                {
                    bool reduced = await medications.TryReduceQuantityAsync(
                        medication.Id,
                        request.Quantity,
                        ct);
                    if (!reduced)
                    {
                        return Result<bool>.Failure(
                            AppError.Validation("Quantidade insuficiente em estoque."));
                    }

                    Medication destinationMedication = medication.CopyForTransfer(
                        request.Quantity,
                        destinationLocation.Name,
                        destinationLocation.Id);

                    await medications.AddAsync(destinationMedication, ct);
                    await medications.SaveChangesAsync(ct);
                    newMedicationId = destinationMedication.Id;
                }

                movement = StockMovement.Create(
                    "transferencia",
                    medication.Id,
                    request.Quantity,
                    request.Date,
                    request.Responsible,
                    notes,
                    medication.Batch,
                    "Transferencia de local");

                await movements.AddAsync(movement, ct);

                return Result<bool>.Success(true);
            },
            cancellationToken);

        return !transferResult.IsSuccess
            ? Result<TransferMedicationResponse>.Failure(transferResult.Error!)
            : Result<TransferMedicationResponse>.Success(
                new TransferMedicationResponse(true, medication.Id, newMedicationId, movement!.Id));
    }
}
