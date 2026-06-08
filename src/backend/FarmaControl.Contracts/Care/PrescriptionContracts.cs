namespace FarmaControl.Contracts.Care;

public sealed record CreatePrescriptionRequest(
    long MedicalRecordId,
    long PatientId,
    long? MedicationId,
    string? MedicationName,
    string? Dosage,
    string? Directions,
    int? Quantity,
    string? Notes);

public sealed record DispensePrescriptionRequest(
    string? Responsible,
    long? MedicationId = null);

public sealed record PrescriptionResponse(
    long Id,
    long MedicalRecordId,
    long PatientId,
    long? MedicationId,
    string? MedicationName,
    string? Dosage,
    string? Directions,
    int Quantity,
    bool IsDispensed,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DispensedAt);

public sealed record DispensePrescriptionResponse(
    bool Ok,
    long PrescriptionId,
    long MedicationId,
    long MovementId,
    long? MedicalAttendanceId,
    string? Batch,
    DateOnly? ExpirationDate);
