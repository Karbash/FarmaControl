using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Application.Users.Abstractions;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;
using FarmaControl.Domain.Users;

namespace FarmaControl.Application.Inventory.UseCases;

public sealed class ListMedicationsUseCase(IMedicationRepository medications)
    : IUseCase<NoRequest, IReadOnlyList<MedicationResponse>>
{
    public async Task<IReadOnlyList<MedicationResponse>> ExecuteAsync(
        NoRequest request,
        CancellationToken cancellationToken)
    {
        var result = await medications.ListAsync(cancellationToken);
        return result.Select(MedicationModel.FromDomain).ToArray();
    }
}

public sealed record GetMedicationRequest(long Id);

public sealed class GetMedicationUseCase(IMedicationRepository medications)
    : IUseCase<GetMedicationRequest, Result<MedicationResponse>>
{
    public async Task<Result<MedicationResponse>> ExecuteAsync(
        GetMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var medication = await medications.GetByIdAsync(request.Id, cancellationToken);
        return medication is null
            ? Result<MedicationResponse>.Failure(AppError.NotFound("Medicamento nao encontrado."))
            : Result<MedicationResponse>.Success(MedicationModel.FromDomain(medication));
    }
}

public sealed record CreateMedicationCommand(
    long ActorUserId,
    MedicationInputModel Model,
    string? SignaturePassword);

public sealed class CreateMedicationUseCase(
    IMedicationRepository medications,
    IDonorRepository donors,
    IManufacturerRepository manufacturers,
    IStockLocationRepository locations,
    IUserRepository users,
    IPasswordHasher passwordHasher)
    : IUseCase<CreateMedicationCommand, Result<MedicationResponse>>
{
    public async Task<Result<MedicationResponse>> ExecuteAsync(
        CreateMedicationCommand request,
        CancellationToken cancellationToken)
    {
        User? actor = await users.GetByIdAsync(request.ActorUserId, cancellationToken);
        AppError? signatureError = InventorySignatureGuard.ValidateSignaturePassword(
            actor,
            request.SignaturePassword,
            passwordHasher);
        if (signatureError is not null)
        {
            return Result<MedicationResponse>.Failure(signatureError);
        }

        IReadOnlyList<AppError> errors = request.Model.Validate();
        if (errors.HasErrors())
        {
            return Result<MedicationResponse>.Failure(errors.FirstOrDefaultError());
        }

        Result<ResolvedMedicationReferences> references = await MedicationReferenceResolver.ResolveReferencesAsync(
            request.Model,
            donors,
            manufacturers,
            locations,
            cancellationToken);
        if (!references.IsSuccess || references.Value is null)
        {
            return Result<MedicationResponse>.Failure(references.Error!);
        }

        var medication = request.Model.ToDomain(
            references.Value.Origin,
            references.Value.Manufacturer,
            references.Value.Location);
        await medications.AddAsync(medication, cancellationToken);
        await medications.SaveChangesAsync(cancellationToken);

        return Result<MedicationResponse>.Success(MedicationModel.FromDomain(medication));
    }
}

internal static class InventorySignatureGuard
{
    public static AppError? ValidateSignaturePassword(
        User? actor,
        string? signaturePassword,
        IPasswordHasher passwordHasher)
    {
        if (actor is null || !actor.CanAuthenticate)
        {
            return AppError.Forbidden("Usuario nao autenticado.");
        }

        if (actor.SignaturePasswordResetRequired || string.IsNullOrWhiteSpace(actor.SignaturePasswordHash))
        {
            return AppError.Forbidden("Senha de assinatura pendente de cadastro.");
        }

        if (string.IsNullOrWhiteSpace(signaturePassword))
        {
            return AppError.Validation("Senha de assinatura e obrigatoria.");
        }

        return passwordHasher.VerifyHash(actor.SignaturePasswordHash, signaturePassword)
            ? null
            : AppError.Forbidden("Senha de assinatura incorreta.");
    }
}

public sealed record UpdateMedicationCommand(long Id, MedicationInputModel Model);

public sealed class UpdateMedicationUseCase(
    IMedicationRepository medications,
    IDonorRepository donors,
    IManufacturerRepository manufacturers,
    IStockLocationRepository locations)
    : IUseCase<UpdateMedicationCommand, Result<MedicationResponse>>
{
    public async Task<Result<MedicationResponse>> ExecuteAsync(
        UpdateMedicationCommand request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Model.Validate();
        if (errors.HasErrors())
        {
            return Result<MedicationResponse>.Failure(errors.FirstOrDefaultError());
        }

        var medication = await medications.GetByIdAsync(request.Id, cancellationToken);
        if (medication is null)
        {
            return Result<MedicationResponse>.Failure(AppError.NotFound("Medicamento nao encontrado."));
        }

        Result<ResolvedMedicationReferences> references = await MedicationReferenceResolver.ResolveReferencesAsync(
            request.Model,
            donors,
            manufacturers,
            locations,
            cancellationToken);
        if (!references.IsSuccess || references.Value is null)
        {
            return Result<MedicationResponse>.Failure(references.Error!);
        }

        request.Model.ApplyTo(
            medication,
            references.Value.Origin,
            references.Value.Manufacturer,
            references.Value.Location);
        await medications.SaveChangesAsync(cancellationToken);

        return Result<MedicationResponse>.Success(MedicationModel.FromDomain(medication));
    }
}

internal sealed record ResolvedMedicationReferences(
    string? Origin,
    string? Manufacturer,
    string Location);

internal static class MedicationReferenceResolver
{
    public static async Task<Result<ResolvedMedicationReferences>> ResolveReferencesAsync(
        MedicationInputModel model,
        IDonorRepository donors,
        IManufacturerRepository manufacturers,
        IStockLocationRepository locations,
        CancellationToken cancellationToken)
    {
        StockLocation? location = model.LocationId.HasValue
            ? await locations.GetByIdAsync(model.LocationId.Value, cancellationToken)
            : null;
        if (location is null)
        {
            return Result<ResolvedMedicationReferences>.Failure(
                AppError.Validation("Local do lote nao encontrado."));
        }

        string? origin = null;
        if (model.OriginId.HasValue)
        {
            Donor? donor = await donors.GetByIdAsync(model.OriginId.Value, cancellationToken);
            if (donor is null)
            {
                return Result<ResolvedMedicationReferences>.Failure(
                    AppError.Validation("Origem/doador nao encontrado."));
            }

            origin = donor.Name;
        }

        string? manufacturerName = null;
        if (model.ManufacturerId.HasValue)
        {
            Manufacturer? manufacturer = await manufacturers.GetByIdAsync(
                model.ManufacturerId.Value,
                cancellationToken);
            if (manufacturer is null)
            {
                return Result<ResolvedMedicationReferences>.Failure(
                    AppError.Validation("Fabricante nao encontrado."));
            }

            manufacturerName = manufacturer.Name;
        }

        return Result<ResolvedMedicationReferences>.Success(
            new ResolvedMedicationReferences(origin, manufacturerName, location.Name));
    }
}

public sealed record DeleteMedicationCommand(long Id);

public sealed class DeleteMedicationUseCase(IMedicationRepository medications)
    : IUseCase<DeleteMedicationCommand, Result<bool>>
{
    public async Task<Result<bool>> ExecuteAsync(
        DeleteMedicationCommand request,
        CancellationToken cancellationToken)
    {
        var medication = await medications.GetByIdAsync(request.Id, cancellationToken);
        if (medication is null)
        {
            return Result<bool>.Failure(AppError.NotFound("Medicamento nao encontrado."));
        }

        medications.Remove(medication);
        await medications.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
