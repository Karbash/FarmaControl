using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.MedicalAttendances.Models;

public sealed record PrescriptionItemModel(
    int Order,
    string? Description,
    long? MedicationId,
    string? MedicationName,
    string? Dosage,
    string? Directions,
    int? Quantity) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (Order <= 0)
        {
            errors.Add(AppError.Validation("Ordem da prescricao deve ser maior que zero."));
        }

        if (Quantity is <= 0)
        {
            errors.Add(AppError.Validation("Quantidade da prescricao deve ser maior que zero."));
        }

        return errors;
    }

    public MedicalAttendancePrescriptionItem ToDomain()
    {
        return MedicalAttendancePrescriptionItem.Create(
            Order,
            Description,
            MedicationId,
            MedicationName,
            Dosage,
            Directions,
            Quantity);
    }

    public static PrescriptionItemModel FromRequest(MedicalAttendancePrescriptionItemRequest request)
    {
        return new PrescriptionItemModel(
            request.Order,
            request.Description,
            request.MedicationId,
            request.MedicationName,
            request.Dosage,
            request.Directions,
            request.Quantity);
    }

    public static MedicalAttendancePrescriptionItemResponse FromDomain(
        MedicalAttendancePrescriptionItem item)
    {
        return new MedicalAttendancePrescriptionItemResponse(
            item.Id,
            item.Order,
            item.Description,
            item.MedicationId,
            item.MedicationName,
            item.Dosage,
            item.Directions,
            item.Quantity);
    }
}

public sealed record NursingCheckItemModel(int Order, string? Description) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (Order <= 0)
        {
            errors.Add(AppError.Validation("Ordem da checagem deve ser maior que zero."));
        }

        return errors;
    }

    public MedicalAttendanceNursingCheckItem ToDomain()
    {
        return MedicalAttendanceNursingCheckItem.Create(Order, Description);
    }

    public static NursingCheckItemModel FromRequest(MedicalAttendanceNursingCheckItemRequest request)
    {
        return new NursingCheckItemModel(request.Order, request.Description);
    }

    public static MedicalAttendanceNursingCheckItemResponse FromDomain(
        MedicalAttendanceNursingCheckItem item)
    {
        return new MedicalAttendanceNursingCheckItemResponse(
            item.Id,
            item.Order,
            item.Description);
    }
}

public sealed record DispensationItemModel(
    int Order,
    string? Batch,
    long? PrescriptionId,
    long? MedicationId,
    string? MedicationName,
    int? Quantity,
    string? Responsible,
    DateTimeOffset? DispensedAt) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (Order <= 0)
        {
            errors.Add(AppError.Validation("Ordem da dispensacao deve ser maior que zero."));
        }

        return errors;
    }

    public MedicalAttendanceDispensationItem ToDomain()
    {
        if (PrescriptionId.HasValue && MedicationId.HasValue && Quantity.HasValue && DispensedAt.HasValue)
        {
            return MedicalAttendanceDispensationItem.CreateFromPrescription(
                Order,
                Batch,
                PrescriptionId.Value,
                MedicationId.Value,
                MedicationName,
                Quantity.Value,
                Responsible ?? "Sistema",
                DispensedAt.Value);
        }

        return MedicalAttendanceDispensationItem.Create(Order, Batch);
    }

    public static DispensationItemModel FromRequest(MedicalAttendanceDispensationItemRequest request)
    {
        return new DispensationItemModel(
            request.Order,
            request.Batch,
            request.PrescriptionId,
            request.MedicationId,
            request.MedicationName,
            request.Quantity,
            request.Responsible,
            request.DispensedAt);
    }

    public static MedicalAttendanceDispensationItemResponse FromDomain(
        MedicalAttendanceDispensationItem item)
    {
        return new MedicalAttendanceDispensationItemResponse(
            item.Id,
            item.Order,
            item.Batch,
            item.PrescriptionId,
            item.MedicationId,
            item.MedicationName,
            item.Quantity,
            item.Responsible,
            item.DispensedAt);
    }
}
