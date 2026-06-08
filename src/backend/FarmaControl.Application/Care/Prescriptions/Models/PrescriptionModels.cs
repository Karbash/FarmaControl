using FarmaControl.Application.Abstractions;
using FarmaControl.Contracts.Care;
using FarmaControl.Domain.Care;

namespace FarmaControl.Application.Care.Prescriptions.Models;

public sealed record PrescriptionInputModel(
    long MedicalRecordId,
    long PatientId,
    long? MedicationId,
    string? MedicationName,
    string? Dosage,
    string? Directions,
    int? Quantity,
    string? Notes) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        var errors = new List<AppError>();

        if (MedicalRecordId <= 0)
        {
            errors.Add(AppError.Validation("Prontuario e obrigatorio."));
        }

        if (PatientId <= 0)
        {
            errors.Add(AppError.Validation("Paciente e obrigatorio."));
        }

        if (Quantity is <= 0)
        {
            errors.Add(AppError.Validation("Quantidade deve ser maior que zero."));
        }

        return errors;
    }

    public Prescription ToDomain()
    {
        return Prescription.Create(
            MedicalRecordId,
            PatientId,
            MedicationId,
            MedicationName,
            Dosage,
            Directions,
            Quantity ?? 1,
            Notes);
    }

    public static PrescriptionInputModel FromRequest(CreatePrescriptionRequest request)
    {
        return new PrescriptionInputModel(request.MedicalRecordId, request.PatientId, request.MedicationId, request.MedicationName, request.Dosage, request.Directions, request.Quantity, request.Notes);
    }
}

public sealed record DispensePrescriptionModel(
    long PrescriptionId,
    string? Responsible,
    long? MedicationId) : IExplicitModel
{
    public IReadOnlyList<AppError> Validate()
    {
        return PrescriptionId <= 0
            ? [AppError.Validation("Prescricao e obrigatoria.")]
            : [];
    }

    public static DispensePrescriptionModel FromRequest(long prescriptionId, DispensePrescriptionRequest request)
    {
        return new DispensePrescriptionModel(prescriptionId, request.Responsible, request.MedicationId);
    }
}

public static class PrescriptionModel
{
    public static PrescriptionResponse FromDomain(Prescription prescription)
    {
        return new PrescriptionResponse(
            prescription.Id,
            prescription.MedicalRecordId,
            prescription.PatientId,
            prescription.MedicationId,
            prescription.MedicationName,
            prescription.Dosage,
            prescription.Directions,
            prescription.Quantity,
            prescription.IsDispensed,
            prescription.Notes,
            prescription.CreatedAt,
            prescription.DispensedAt);
    }
}
