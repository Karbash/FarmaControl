using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Inventory;
using FarmaControl.Domain.Inventory;

namespace FarmaControl.Application.Inventory.Models;

public sealed record MedicationInputModel(
    string? GenericName,
    string? CommercialName,
    string? TherapeuticClass,
    string? PharmaceuticalForm,
    string? Dosage,
    DateOnly? EntryDate,
    string? Origin,
    long? OriginId,
    string? Responsible,
    string? Manufacturer,
    long? ManufacturerId,
    string? Batch,
    DateOnly? ExpirationDate,
    int Quantity,
    string? Unit,
    string? Location,
    long? LocationId,
    int? MinimumQuantity,
    bool IsControlled) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (string.IsNullOrWhiteSpace(GenericName) && string.IsNullOrWhiteSpace(CommercialName))
        {
            errors.Add(AppError.Validation("Informe nome generico ou nome comercial."));
        }

        if (Quantity < 0)
        {
            errors.Add(AppError.Validation("Quantidade nao pode ser negativa."));
        }

        if (MinimumQuantity is < 0)
        {
            errors.Add(AppError.Validation("Quantidade minima nao pode ser negativa."));
        }

        if (LocationId is null or <= 0)
        {
            errors.Add(AppError.Validation("Local do lote e obrigatorio."));
        }

        if (OriginId is <= 0)
        {
            errors.Add(AppError.Validation("Origem invalida."));
        }

        if (ManufacturerId is <= 0)
        {
            errors.Add(AppError.Validation("Fabricante invalido."));
        }

        return errors;
    }

    public Medication ToDomain(
        string? origin,
        string? manufacturer,
        string? location)
    {
        return Medication.Create(
            GenericName,
            CommercialName,
            TherapeuticClass,
            PharmaceuticalForm,
            Dosage,
            EntryDate,
            origin,
            OriginId,
            Responsible,
            manufacturer,
            ManufacturerId,
            Batch,
            ExpirationDate,
            Quantity,
            Unit,
            location,
            LocationId,
            MinimumQuantity ?? 5,
            IsControlled);
    }

    public void ApplyTo(
        Medication medication,
        string? origin,
        string? manufacturer,
        string? location)
    {
        medication.Update(
            GenericName,
            CommercialName,
            TherapeuticClass,
            PharmaceuticalForm,
            Dosage,
            EntryDate,
            origin,
            OriginId,
            Responsible,
            manufacturer,
            ManufacturerId,
            Batch,
            ExpirationDate,
            Quantity,
            Unit,
            location,
            LocationId,
            MinimumQuantity ?? 5,
            IsControlled);
    }

    public static MedicationInputModel FromRequest(CreateMedicationRequest request)
    {
        return new MedicationInputModel(
            request.GenericName,
            request.CommercialName,
            request.TherapeuticClass,
            request.PharmaceuticalForm,
            request.Dosage,
            request.EntryDate,
            request.Origin,
            request.OriginId,
            request.Responsible,
            request.Manufacturer,
            request.ManufacturerId,
            request.Batch,
            request.ExpirationDate,
            request.Quantity,
            request.Unit,
            request.Location,
            request.LocationId,
            request.MinimumQuantity,
            request.IsControlled);
    }

    public static MedicationInputModel FromRequest(UpdateMedicationRequest request)
    {
        return new MedicationInputModel(
            request.GenericName,
            request.CommercialName,
            request.TherapeuticClass,
            request.PharmaceuticalForm,
            request.Dosage,
            request.EntryDate,
            request.Origin,
            request.OriginId,
            request.Responsible,
            request.Manufacturer,
            request.ManufacturerId,
            request.Batch,
            request.ExpirationDate,
            request.Quantity,
            request.Unit,
            request.Location,
            request.LocationId,
            request.MinimumQuantity,
            request.IsControlled);
    }
}

public static class MedicationModel
{
    public static MedicationResponse FromDomain(Medication medication)
    {
        return new MedicationResponse(
            medication.Id,
            medication.GenericName,
            medication.CommercialName,
            medication.TherapeuticClass,
            medication.PharmaceuticalForm,
            medication.Dosage,
            medication.EntryDate,
            medication.Origin,
            medication.OriginId,
            medication.Responsible,
            medication.Manufacturer,
            medication.ManufacturerId,
            medication.Batch,
            medication.ExpirationDate,
            medication.Quantity,
            medication.Unit,
            medication.Location,
            medication.LocationId,
            medication.MinimumQuantity,
            medication.IsControlled,
            medication.CreatedAt,
            medication.UpdatedAt);
    }
}
