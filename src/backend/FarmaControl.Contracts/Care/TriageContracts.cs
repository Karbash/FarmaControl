namespace FarmaControl.Contracts.Care;

public sealed record CreateTriageRecordRequest(
    long AppointmentId,
    string? BloodPressure,
    string? Temperature,
    string? Weight,
    string? Height,
    string? HeartRate,
    string? OxygenSaturation,
    string? ChiefComplaint,
    string? Responsible,
    string? Notes);

public sealed record UpdateTriageRecordRequest(
    string? BloodPressure,
    string? Temperature,
    string? Weight,
    string? Height,
    string? HeartRate,
    string? OxygenSaturation,
    string? ChiefComplaint,
    string? Responsible,
    string? Notes);

public sealed record TriageRecordResponse(
    long Id,
    long AppointmentId,
    string? BloodPressure,
    string? Temperature,
    string? Weight,
    string? Height,
    string? HeartRate,
    string? OxygenSaturation,
    string? ChiefComplaint,
    string? Responsible,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
