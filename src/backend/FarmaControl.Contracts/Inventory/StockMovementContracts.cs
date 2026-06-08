namespace FarmaControl.Contracts.Inventory;

public sealed record CreateStockMovementRequest(
    string Type,
    long MedicationId,
    int Quantity,
    DateOnly Date,
    string Responsible,
    string? Notes,
    string? Batch,
    string? Reason,
    long? AttendanceId = null,
    long? AppointmentId = null,
    long? PrescriptionId = null);

public sealed record TransferMedicationRequest(
    long MedicationId,
    string DestinationLocation,
    long? DestinationLocationId,
    int Quantity,
    string Responsible,
    DateOnly Date,
    string? Notes);

public sealed record StockMovementResponse(
    long Id,
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
    long? PrescriptionId,
    DateTimeOffset CreatedAt);

public sealed record TransferMedicationResponse(
    bool Ok,
    long MedicationId,
    long? NewMedicationId,
    long MovementId);
