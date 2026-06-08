using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Users;

namespace FarmaControl.Application.Users.Models;

public sealed record RevokeUserAccessModel(long UserId, long RevokedByUserId, string? Reason)
    : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (UserId <= 0)
        {
            errors.Add(AppError.Validation("Usuario e obrigatorio."));
        }

        if (RevokedByUserId <= 0)
        {
            errors.Add(AppError.Validation("Usuario responsavel e obrigatorio."));
        }

        return errors;
    }

    public static RevokeUserAccessModel FromRequest(
        long userId,
        long revokedByUserId,
        RevokeUserAccessRequest request)
    {
        return new RevokeUserAccessModel(userId, revokedByUserId, request.Reason);
    }
}

public sealed record RevokeUserModuleModel(
    long UserId,
    string Module,
    long RevokedByUserId,
    string? Reason) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (UserId <= 0)
        {
            errors.Add(AppError.Validation("Usuario e obrigatorio."));
        }

        if (string.IsNullOrWhiteSpace(Module))
        {
            errors.Add(AppError.Validation("Modulo e obrigatorio."));
        }

        if (RevokedByUserId <= 0)
        {
            errors.Add(AppError.Validation("Usuario responsavel e obrigatorio."));
        }

        return errors;
    }
}
