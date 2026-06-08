using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Auth;

namespace FarmaControl.Application.Users.Models;

public sealed record ChangePasswordModel(
    long UserId,
    string CurrentPassword,
    string NewPassword) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (UserId <= 0)
        {
            errors.Add(AppError.Validation("Usuario e obrigatorio."));
        }

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            errors.Add(AppError.Validation("Senha atual e obrigatoria."));
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            errors.Add(AppError.Validation("Nova senha e obrigatoria."));
        }

        return errors;
    }

    public static ChangePasswordModel FromRequest(long userId, ChangePasswordRequest request)
    {
        return new ChangePasswordModel(userId, request.CurrentPassword, request.NewPassword);
    }
}

public sealed record ChangeSignaturePasswordModel(
    long UserId,
    string? CurrentPassword,
    string? CurrentSignaturePassword,
    string NewSignaturePassword) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (UserId <= 0)
        {
            errors.Add(AppError.Validation("Usuario e obrigatorio."));
        }

        if (string.IsNullOrWhiteSpace(NewSignaturePassword))
        {
            errors.Add(AppError.Validation("Nova senha de assinatura e obrigatoria."));
        }

        return errors;
    }

    public static ChangeSignaturePasswordModel FromRequest(
        long userId,
        ChangeSignaturePasswordRequest request)
    {
        return new ChangeSignaturePasswordModel(
            userId,
            request.CurrentPassword,
            request.CurrentSignaturePassword,
            request.NewSignaturePassword);
    }
}
