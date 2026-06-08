using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Inventory;

public sealed class Medication : Entity
{
    private Medication()
    {
    }

    private Medication(
        string? genericName,
        string? commercialName,
        string? therapeuticClass,
        string? pharmaceuticalForm,
        string? dosage,
        DateOnly? entryDate,
        string? origin,
        long? originId,
        string? responsible,
        string? manufacturer,
        long? manufacturerId,
        string? batch,
        DateOnly? expirationDate,
        int quantity,
        string? unit,
        string? location,
        long? locationId,
        int minimumQuantity,
        bool isControlled)
    {
        Update(
            genericName,
            commercialName,
            therapeuticClass,
            pharmaceuticalForm,
            dosage,
            entryDate,
            origin,
            originId,
            responsible,
            manufacturer,
            manufacturerId,
            batch,
            expirationDate,
            quantity,
            unit,
            location,
            locationId,
            minimumQuantity,
            isControlled);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string? GenericName { get; private set; }

    public string? CommercialName { get; private set; }

    public string? TherapeuticClass { get; private set; }

    public string? PharmaceuticalForm { get; private set; }

    public string? Dosage { get; private set; }

    public DateOnly? EntryDate { get; private set; }

    public string? Origin { get; private set; }

    public long? OriginId { get; private set; }

    public string? Responsible { get; private set; }

    public string? Manufacturer { get; private set; }

    public long? ManufacturerId { get; private set; }

    public string? Batch { get; private set; }

    public DateOnly? ExpirationDate { get; private set; }

    public int Quantity { get; private set; }

    public string? Unit { get; private set; }

    public string? Location { get; private set; }

    public long? LocationId { get; private set; }

    public int MinimumQuantity { get; private set; }

    public bool IsControlled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Medication Create(
        string? genericName,
        string? commercialName,
        string? therapeuticClass,
        string? pharmaceuticalForm,
        string? dosage,
        DateOnly? entryDate,
        string? origin,
        long? originId,
        string? responsible,
        string? manufacturer,
        long? manufacturerId,
        string? batch,
        DateOnly? expirationDate,
        int quantity,
        string? unit,
        string? location,
        long? locationId,
        int minimumQuantity,
        bool isControlled)
    {
        return new Medication(
            genericName,
            commercialName,
            therapeuticClass,
            pharmaceuticalForm,
            dosage,
            entryDate,
            origin,
            originId,
            responsible,
            manufacturer,
            manufacturerId,
            batch,
            expirationDate,
            quantity,
            unit,
            location,
            locationId,
            minimumQuantity,
            isControlled);
    }

    public void Update(
        string? genericName,
        string? commercialName,
        string? therapeuticClass,
        string? pharmaceuticalForm,
        string? dosage,
        DateOnly? entryDate,
        string? origin,
        long? originId,
        string? responsible,
        string? manufacturer,
        long? manufacturerId,
        string? batch,
        DateOnly? expirationDate,
        int quantity,
        string? unit,
        string? location,
        long? locationId,
        int minimumQuantity,
        bool isControlled)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantidade nao pode ser negativa.", nameof(quantity));
        }

        if (minimumQuantity < 0)
        {
            throw new ArgumentException("Quantidade minima nao pode ser negativa.", nameof(minimumQuantity));
        }

        GenericName = Normalize(genericName);
        CommercialName = Normalize(commercialName);
        TherapeuticClass = Normalize(therapeuticClass);
        PharmaceuticalForm = Normalize(pharmaceuticalForm);
        Dosage = Normalize(dosage);
        EntryDate = entryDate;
        Origin = Normalize(origin);
        OriginId = originId;
        Responsible = Normalize(responsible);
        Manufacturer = Normalize(manufacturer);
        ManufacturerId = manufacturerId;
        Batch = Normalize(batch);
        ExpirationDate = expirationDate;
        Quantity = quantity;
        Unit = Normalize(unit);
        Location = Normalize(location);
        LocationId = locationId;
        MinimumQuantity = minimumQuantity;
        IsControlled = isControlled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Medication CopyForTransfer(int quantity, string destinationLocation, long? destinationLocationId)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));
        }

        if (string.IsNullOrWhiteSpace(destinationLocation))
        {
            throw new ArgumentException("Destino e obrigatorio.", nameof(destinationLocation));
        }

        return Create(
            GenericName,
            CommercialName,
            TherapeuticClass,
            PharmaceuticalForm,
            Dosage,
            EntryDate,
            Origin,
            OriginId,
            Responsible,
            Manufacturer,
            ManufacturerId,
            Batch,
            ExpirationDate,
            quantity,
            Unit,
            destinationLocation,
            destinationLocationId,
            MinimumQuantity,
            IsControlled);
    }

    public void ChangeLocation(string destinationLocation, long? destinationLocationId)
    {
        if (string.IsNullOrWhiteSpace(destinationLocation))
        {
            throw new ArgumentException("Destino e obrigatorio.", nameof(destinationLocation));
        }

        Location = destinationLocation.Trim();
        LocationId = destinationLocationId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReduceQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));
        }

        if (quantity > Quantity)
        {
            throw new InvalidOperationException("Quantidade insuficiente em estoque.");
        }

        Quantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
