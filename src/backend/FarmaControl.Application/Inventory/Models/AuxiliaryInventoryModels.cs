using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Inventory.Models;

public sealed record CreateDonorModel(string Name, string? Phone, string? Notes) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? [AppError.Validation("Nome e obrigatorio.")]
            : [];
    }

    public Donor ToDomain() => Donor.Create(Name, Phone, Notes);

    public static CreateDonorModel FromRequest(CreateDonorRequest request) =>
        new(request.Name, request.Phone, request.Notes);
}

public sealed record UpdateDonorModel(string Name, string? Phone, string? Notes) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? [AppError.Validation("Nome e obrigatorio.")]
            : [];
    }

    public void Apply(Donor donor) => donor.Update(Name, Phone, Notes);

    public static UpdateDonorModel FromRequest(UpdateDonorRequest request) =>
        new(request.Name, request.Phone, request.Notes);
}

public static class DonorModel
{
    public static DonorResponse FromDomain(Donor donor) =>
        new(donor.Id, donor.Name, donor.Phone, donor.Notes, donor.CreatedAt);
}

public sealed record CreateManufacturerModel(string Name, string? Cnpj) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? [AppError.Validation("Nome e obrigatorio.")]
            : [];
    }

    public Manufacturer ToDomain() => Manufacturer.Create(Name, Cnpj);

    public static CreateManufacturerModel FromRequest(CreateManufacturerRequest request) =>
        new(request.Name, request.Cnpj);
}

public sealed record UpdateManufacturerModel(string Name, string? Cnpj) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? [AppError.Validation("Nome e obrigatorio.")]
            : [];
    }

    public void Apply(Manufacturer manufacturer) => manufacturer.Update(Name, Cnpj);

    public static UpdateManufacturerModel FromRequest(UpdateManufacturerRequest request) =>
        new(request.Name, request.Cnpj);
}

public static class ManufacturerModel
{
    public static ManufacturerResponse FromDomain(Manufacturer manufacturer) =>
        new(manufacturer.Id, manufacturer.Name, manufacturer.Cnpj, manufacturer.CreatedAt);
}

public sealed record CreateStockLocationModel(string Name) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? [AppError.Validation("Nome e obrigatorio.")]
            : [];
    }

    public StockLocation ToDomain() => StockLocation.Create(Name);

    public static CreateStockLocationModel FromRequest(CreateStockLocationRequest request) =>
        new(request.Name);
}

public sealed record UpdateStockLocationModel(string Name) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return string.IsNullOrWhiteSpace(Name)
            ? [AppError.Validation("Nome e obrigatorio.")]
            : [];
    }

    public void Apply(StockLocation location) => location.Update(Name);

    public static UpdateStockLocationModel FromRequest(UpdateStockLocationRequest request) =>
        new(request.Name);
}

public static class StockLocationModel
{
    public static StockLocationResponse FromDomain(StockLocation location) =>
        new(location.Id, location.Name, location.CreatedAt);
}
