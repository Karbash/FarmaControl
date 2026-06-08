using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Inventory.Models;

public sealed record StockMovementInputModel(
    string Type,
    long MedicationId,
    int Quantity,
    DateOnly Date,
    string Responsible,
    string? Notes,
    string? Batch,
    string? Reason,
    long? AttendanceId,
    long? AppointmentId,
    long? PrescriptionId) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (string.IsNullOrWhiteSpace(Type))
        {
            errors.Add(AppError.Validation("Tipo e obrigatorio."));
        }

        if (MedicationId <= 0)
        {
            errors.Add(AppError.Validation("Medicamento e obrigatorio."));
        }

        if (Quantity <= 0)
        {
            errors.Add(AppError.Validation("Quantidade deve ser maior que zero."));
        }

        if (string.IsNullOrWhiteSpace(Responsible))
        {
            errors.Add(AppError.Validation("Responsavel e obrigatorio."));
        }

        return errors;
    }

    public StockMovement ToDomain()
    {
        return StockMovement.Create(
            Type,
            MedicationId,
            Quantity,
            Date,
            Responsible,
            Notes,
            Batch,
            Reason,
            AttendanceId,
            AppointmentId,
            PrescriptionId);
    }

    public static StockMovementInputModel FromRequest(CreateStockMovementRequest request)
    {
        return new StockMovementInputModel(
            request.Type,
            request.MedicationId,
            request.Quantity,
            request.Date,
            request.Responsible,
            request.Notes,
            request.Batch,
            request.Reason,
            request.AttendanceId,
            request.AppointmentId,
            request.PrescriptionId);
    }
}

public sealed record TransferMedicationModel(
    long MedicationId,
    string DestinationLocation,
    long? DestinationLocationId,
    int Quantity,
    string Responsible,
    DateOnly Date,
    string? Notes) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (MedicationId <= 0)
        {
            errors.Add(AppError.Validation("Medicamento e obrigatorio."));
        }

        if (DestinationLocationId is null or <= 0)
        {
            errors.Add(AppError.Validation("Destino e obrigatorio."));
        }

        if (Quantity <= 0)
        {
            errors.Add(AppError.Validation("Quantidade deve ser maior que zero."));
        }

        if (string.IsNullOrWhiteSpace(Responsible))
        {
            errors.Add(AppError.Validation("Responsavel e obrigatorio."));
        }

        return errors;
    }

    public static TransferMedicationModel FromRequest(TransferMedicationRequest request)
    {
        return new TransferMedicationModel(
            request.MedicationId,
            request.DestinationLocation,
            request.DestinationLocationId,
            request.Quantity,
            request.Responsible,
            request.Date,
            request.Notes);
    }
}

public static class StockMovementModel
{
    public static StockMovementResponse FromDomain(StockMovement movement)
    {
        return new StockMovementResponse(
            movement.Id,
            movement.Type,
            movement.MedicationId,
            movement.Quantity,
            movement.Date,
            movement.Responsible,
            movement.Notes,
            movement.Batch,
            movement.Reason,
            movement.AttendanceId,
            movement.AppointmentId,
            movement.PrescriptionId,
            movement.CreatedAt);
    }
}
