using FarmaControl.Application.Abstractions;
using FarmaControl.Application.Inventory.Abstractions;
using FarmaControl.Application.Inventory.Models;
using FarmaControl.Contracts.Inventory;

namespace FarmaControl.Application.Inventory.UseCases;

public sealed class ListDonorsUseCase(IDonorRepository donors)
    : IUseCase<NoRequest, IReadOnlyList<DonorResponse>>
{
    public async Task<IReadOnlyList<DonorResponse>> ExecuteAsync(NoRequest request, CancellationToken cancellationToken)
    {
        var result = await donors.ListAsync(cancellationToken);
        return result.Select(DonorModel.FromDomain).ToArray();
    }
}

public sealed class CreateDonorUseCase(IDonorRepository donors)
    : IUseCase<CreateDonorModel, Result<DonorResponse>>
{
    public async Task<Result<DonorResponse>> ExecuteAsync(CreateDonorModel request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<DonorResponse>.Failure(errors.FirstOrDefaultError());
        }

        var donor = request.ToDomain();
        await donors.AddAsync(donor, cancellationToken);
        await donors.SaveChangesAsync(cancellationToken);

        return Result<DonorResponse>.Success(DonorModel.FromDomain(donor));
    }
}

public sealed record UpdateDonorCommand(long Id, UpdateDonorModel Request);

public sealed class UpdateDonorUseCase(IDonorRepository donors)
    : IUseCase<UpdateDonorCommand, Result<DonorResponse>>
{
    public async Task<Result<DonorResponse>> ExecuteAsync(UpdateDonorCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Request.Validate();
        if (errors.HasErrors())
        {
            return Result<DonorResponse>.Failure(errors.FirstOrDefaultError());
        }

        var donor = await donors.GetByIdAsync(request.Id, cancellationToken);
        if (donor is null)
        {
            return Result<DonorResponse>.Failure(AppError.NotFound("Doador nao encontrado."));
        }

        request.Request.Apply(donor);
        await donors.SaveChangesAsync(cancellationToken);

        return Result<DonorResponse>.Success(DonorModel.FromDomain(donor));
    }
}

public sealed record DeleteDonorCommand(long Id);

public sealed class DeleteDonorUseCase(IDonorRepository donors)
    : IUseCase<DeleteDonorCommand, Result<bool>>
{
    public async Task<Result<bool>> ExecuteAsync(DeleteDonorCommand request, CancellationToken cancellationToken)
    {
        var donor = await donors.GetByIdAsync(request.Id, cancellationToken);
        if (donor is null)
        {
            return Result<bool>.Failure(AppError.NotFound("Doador nao encontrado."));
        }

        donors.Remove(donor);
        await donors.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

public sealed class ListManufacturersUseCase(IManufacturerRepository manufacturers)
    : IUseCase<NoRequest, IReadOnlyList<ManufacturerResponse>>
{
    public async Task<IReadOnlyList<ManufacturerResponse>> ExecuteAsync(NoRequest request, CancellationToken cancellationToken)
    {
        var result = await manufacturers.ListAsync(cancellationToken);
        return result.Select(ManufacturerModel.FromDomain).ToArray();
    }
}

public sealed class CreateManufacturerUseCase(IManufacturerRepository manufacturers)
    : IUseCase<CreateManufacturerModel, Result<ManufacturerResponse>>
{
    public async Task<Result<ManufacturerResponse>> ExecuteAsync(CreateManufacturerModel request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<ManufacturerResponse>.Failure(errors.FirstOrDefaultError());
        }

        var manufacturer = request.ToDomain();
        await manufacturers.AddAsync(manufacturer, cancellationToken);
        await manufacturers.SaveChangesAsync(cancellationToken);

        return Result<ManufacturerResponse>.Success(ManufacturerModel.FromDomain(manufacturer));
    }
}

public sealed record UpdateManufacturerCommand(long Id, UpdateManufacturerModel Request);

public sealed class UpdateManufacturerUseCase(IManufacturerRepository manufacturers)
    : IUseCase<UpdateManufacturerCommand, Result<ManufacturerResponse>>
{
    public async Task<Result<ManufacturerResponse>> ExecuteAsync(UpdateManufacturerCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Request.Validate();
        if (errors.HasErrors())
        {
            return Result<ManufacturerResponse>.Failure(errors.FirstOrDefaultError());
        }

        var manufacturer = await manufacturers.GetByIdAsync(request.Id, cancellationToken);
        if (manufacturer is null)
        {
            return Result<ManufacturerResponse>.Failure(AppError.NotFound("Fabricante nao encontrado."));
        }

        request.Request.Apply(manufacturer);
        await manufacturers.SaveChangesAsync(cancellationToken);

        return Result<ManufacturerResponse>.Success(ManufacturerModel.FromDomain(manufacturer));
    }
}

public sealed record DeleteManufacturerCommand(long Id);

public sealed class DeleteManufacturerUseCase(IManufacturerRepository manufacturers)
    : IUseCase<DeleteManufacturerCommand, Result<bool>>
{
    public async Task<Result<bool>> ExecuteAsync(DeleteManufacturerCommand request, CancellationToken cancellationToken)
    {
        var manufacturer = await manufacturers.GetByIdAsync(request.Id, cancellationToken);
        if (manufacturer is null)
        {
            return Result<bool>.Failure(AppError.NotFound("Fabricante nao encontrado."));
        }

        manufacturers.Remove(manufacturer);
        await manufacturers.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

public sealed class ListStockLocationsUseCase(IStockLocationRepository locations)
    : IUseCase<NoRequest, IReadOnlyList<StockLocationResponse>>
{
    public async Task<IReadOnlyList<StockLocationResponse>> ExecuteAsync(NoRequest request, CancellationToken cancellationToken)
    {
        var result = await locations.ListAsync(cancellationToken);
        return result.Select(StockLocationModel.FromDomain).ToArray();
    }
}

public sealed class CreateStockLocationUseCase(IStockLocationRepository locations)
    : IUseCase<CreateStockLocationModel, Result<StockLocationResponse>>
{
    public async Task<Result<StockLocationResponse>> ExecuteAsync(CreateStockLocationModel request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Validate();
        if (errors.HasErrors())
        {
            return Result<StockLocationResponse>.Failure(errors.FirstOrDefaultError());
        }

        if (await locations.ExistsByNameAsync(request.Name, cancellationToken))
        {
            return Result<StockLocationResponse>.Failure(AppError.Conflict("Local ja cadastrado."));
        }

        var location = request.ToDomain();
        await locations.AddAsync(location, cancellationToken);
        await locations.SaveChangesAsync(cancellationToken);

        return Result<StockLocationResponse>.Success(StockLocationModel.FromDomain(location));
    }
}

public sealed record UpdateStockLocationCommand(long Id, UpdateStockLocationModel Request);

public sealed class UpdateStockLocationUseCase(IStockLocationRepository locations)
    : IUseCase<UpdateStockLocationCommand, Result<StockLocationResponse>>
{
    public async Task<Result<StockLocationResponse>> ExecuteAsync(UpdateStockLocationCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<AppError> errors = request.Request.Validate();
        if (errors.HasErrors())
        {
            return Result<StockLocationResponse>.Failure(errors.FirstOrDefaultError());
        }

        var location = await locations.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
        {
            return Result<StockLocationResponse>.Failure(AppError.NotFound("Local nao encontrado."));
        }

        location.Update(request.Request.Name);
        await locations.SaveChangesAsync(cancellationToken);

        return Result<StockLocationResponse>.Success(StockLocationModel.FromDomain(location));
    }
}

public sealed record DeleteStockLocationCommand(long Id);

public sealed class DeleteStockLocationUseCase(IStockLocationRepository locations)
    : IUseCase<DeleteStockLocationCommand, Result<bool>>
{
    public async Task<Result<bool>> ExecuteAsync(DeleteStockLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await locations.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
        {
            return Result<bool>.Failure(AppError.NotFound("Local nao encontrado."));
        }

        locations.Remove(location);
        await locations.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
