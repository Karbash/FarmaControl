namespace FarmaControl.Contracts.Users;

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    string Role);

public sealed record UpdateUserRequest(
    string Name,
    string Email,
    string? Password,
    string Role,
    bool IsActive);

public sealed record RevokeUserAccessRequest(
    string? Reason);

public sealed record RestoreUserAccessRequest;

public sealed record ResetSignaturePasswordRequest;

public sealed record GrantUserModuleRequest(
    string Module);

public sealed record RevokeUserModuleRequest(
    string? Reason);

public sealed record UserModuleAccessResponse(
    long Id,
    string Module,
    bool IsRevoked,
    DateTimeOffset? RevokedAt,
    long? RevokedByUserId,
    string? RevocationReason,
    long GrantedByUserId,
    DateTimeOffset CreatedAt);

public sealed record UserResponse(
    long Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    bool IsDeleted,
    bool CanAuthenticate,
    bool CanSign,
    bool SignaturePasswordResetRequired,
    DateTimeOffset? AccessRevokedAt,
    long? AccessRevokedByUserId,
    string? AccessRevocationReason,
    DateTimeOffset? DeletedAt,
    long? DeletedByUserId,
    IReadOnlyList<UserModuleAccessResponse> Modules,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ResponsibleUserResponse(
    long Id,
    string Name,
    string Role);
