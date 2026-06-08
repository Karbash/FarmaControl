namespace FarmaControl.Contracts.Health;

public sealed record HealthResponse(
    string Status,
    string Environment,
    bool Database,
    DateTimeOffset CheckedAt);
