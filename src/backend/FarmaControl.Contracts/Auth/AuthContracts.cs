namespace FarmaControl.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record ChangeSignaturePasswordRequest(
    string? CurrentPassword,
    string? CurrentSignaturePassword,
    string NewSignaturePassword);

public sealed record AuthenticatedUserResponse(
    long Id,
    string Name,
    string Email,
    string Role,
    bool SignaturePasswordResetRequired,
    IReadOnlyList<string> Modules,
    string? AccessToken = null,
    DateTimeOffset? AccessTokenExpiresAt = null);
