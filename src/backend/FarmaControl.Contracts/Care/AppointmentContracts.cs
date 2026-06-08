namespace FarmaControl.Contracts.Care;

public sealed record CreateAppointmentRequest(
    long PatientId,
    DateOnly? Date,
    TimeOnly? Time,
    string? Type,
    bool IsEmergency,
    string? Responsible,
    string? Notes);

public sealed record UpdateAppointmentRequest(
    string? Type,
    string? Notes,
    string? DoctorName);

public sealed record UpdateAppointmentStatusRequest(
    string Status,
    string? DoctorName);

public sealed record AppointmentResponse(
    long Id,
    long PatientId,
    DateOnly Date,
    TimeOnly? Time,
    string Type,
    bool IsEmergency,
    string Status,
    string? DoctorName,
    string? Responsible,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
