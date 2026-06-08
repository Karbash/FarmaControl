namespace FarmaControl.Contracts.Care;

public sealed record MedicalAttendancePrescriptionItemRequest(
    int Order,
    string? Description,
    long? MedicationId = null,
    string? MedicationName = null,
    string? Dosage = null,
    string? Directions = null,
    int? Quantity = null);

public sealed record MedicalAttendanceNursingCheckItemRequest(
    int Order,
    string? Description);

public sealed record MedicalAttendanceDispensationItemRequest(
    int Order,
    string? Batch,
    long? PrescriptionId = null,
    long? MedicationId = null,
    string? MedicationName = null,
    int? Quantity = null,
    string? Responsible = null,
    DateTimeOffset? DispensedAt = null);

public sealed record MedicalAttendancePrescriptionItemResponse(
    long Id,
    int Order,
    string? Description,
    long? MedicationId,
    string? MedicationName,
    string? Dosage,
    string? Directions,
    int? Quantity);

public sealed record MedicalAttendanceNursingCheckItemResponse(
    long Id,
    int Order,
    string? Description);

public sealed record MedicalAttendanceDispensationItemResponse(
    long Id,
    int Order,
    string? Batch,
    long? PrescriptionId,
    long? MedicationId,
    string? MedicationName,
    int? Quantity,
    string? Responsible,
    DateTimeOffset? DispensedAt);
