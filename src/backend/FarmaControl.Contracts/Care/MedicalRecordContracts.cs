namespace FarmaControl.Contracts.Care;

public sealed record CreateMedicalRecordRequest(
    long AppointmentId,
    long PatientId,
    string? DoctorName,
    string? Anamnesis,
    string? PhysicalExam,
    string? DiagnosticHypothesis,
    string? Cid10,
    string? Conduct,
    string? Notes);

public sealed record UpdateMedicalRecordRequest(
    string? DoctorName,
    string? Anamnesis,
    string? PhysicalExam,
    string? DiagnosticHypothesis,
    string? Cid10,
    string? Conduct,
    string? Notes);

public sealed record MedicalRecordResponse(
    long Id,
    long AppointmentId,
    long PatientId,
    string? DoctorName,
    string? Anamnesis,
    string? PhysicalExam,
    string? DiagnosticHypothesis,
    string? Cid10,
    string? Conduct,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record Cid10Response(long Id, string Code, string Name);
