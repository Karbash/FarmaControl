using FarmaControl.Domain.Common;

namespace FarmaControl.Domain.Care;

public sealed class Prescription : Entity
{
    private Prescription()
    {
    }

    private Prescription(
        long medicalRecordId,
        long patientId,
        long? medicationId,
        string? medicationName,
        string? dosage,
        string? directions,
        int quantity,
        string? notes)
    {
        if (medicalRecordId <= 0)
        {
            throw new ArgumentException("Prontuario e obrigatorio.", nameof(medicalRecordId));
        }

        if (patientId <= 0)
        {
            throw new ArgumentException("Paciente e obrigatorio.", nameof(patientId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));
        }

        MedicalRecordId = medicalRecordId;
        PatientId = patientId;
        MedicationId = medicationId;
        MedicationName = Normalize(medicationName);
        Dosage = Normalize(dosage);
        Directions = Normalize(directions);
        Quantity = quantity;
        Notes = Normalize(notes);
        IsDispensed = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long MedicalRecordId { get; private set; }
    public long PatientId { get; private set; }
    public long? MedicationId { get; private set; }
    public string? MedicationName { get; private set; }
    public string? Dosage { get; private set; }
    public string? Directions { get; private set; }
    public int Quantity { get; private set; }
    public bool IsDispensed { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DispensedAt { get; private set; }

    public static Prescription Create(
        long medicalRecordId,
        long patientId,
        long? medicationId,
        string? medicationName,
        string? dosage,
        string? directions,
        int quantity,
        string? notes)
    {
        return new Prescription(medicalRecordId, patientId, medicationId, medicationName, dosage, directions, quantity, notes);
    }

    public void MarkDispensed(DateTimeOffset? dispensedAt = null)
    {
        if (IsDispensed)
        {
            throw new InvalidOperationException("Prescricao ja dispensada.");
        }

        IsDispensed = true;
        DispensedAt = dispensedAt ?? DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
