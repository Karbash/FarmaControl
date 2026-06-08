namespace FarmaControl.Contracts.Care;

public sealed record CreatePatientRequest(
    string Name,
    string? Cpf,
    DateOnly? BirthDate,
    string? Sex,
    string? Phone,
    string? Address,
    string? Notes,
    IReadOnlyList<string>? Comorbidities);

public sealed record UpdatePatientRequest(
    string Name,
    string? Cpf,
    DateOnly? BirthDate,
    string? Sex,
    string? Phone,
    string? Address,
    string? Notes,
    IReadOnlyList<string>? Comorbidities,
    bool IsActive);

public sealed record PatientResponse(
    long Id,
    string Name,
    string? Cpf,
    DateOnly? BirthDate,
    string? Sex,
    string? Phone,
    string? Address,
    string? Notes,
    IReadOnlyList<string> Comorbidities,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
