namespace FarmaControl.Contracts.Inventory;

public sealed record CreateDonorRequest(string Name, string? Phone, string? Notes);

public sealed record UpdateDonorRequest(string Name, string? Phone, string? Notes);

public sealed record DonorResponse(long Id, string Name, string? Phone, string? Notes, DateTimeOffset CreatedAt);

public sealed record CreateManufacturerRequest(string Name, string? Cnpj);

public sealed record UpdateManufacturerRequest(string Name, string? Cnpj);

public sealed record ManufacturerResponse(long Id, string Name, string? Cnpj, DateTimeOffset CreatedAt);

public sealed record CreateStockLocationRequest(string Name);

public sealed record UpdateStockLocationRequest(string Name);

public sealed record StockLocationResponse(long Id, string Name, DateTimeOffset CreatedAt);
